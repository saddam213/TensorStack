// Copyright (c) Adam Clark. All rights reserved.
// Licensed under the Apache 2.0 License.
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Pipeline;
using TensorStack.Common.Tensor;
using TensorStack.Common.Video;
using TensorStack.Common.Vision;
using TensorStack.Extractors.Common;
using TensorStack.Extractors.Models;

namespace TensorStack.Extractors.Pipelines
{
    /// <summary>
    /// Basic PosePipeline. This class cannot be inherited.
    /// </summary>
    public class PosePipeline
        : IPipeline<ImageTensor, PoseImageOptions>,
          IPipeline<VideoTensor, PoseVideoOptions>,
          IPipelineStream<VideoFrame, PoseStreamOptions>
    {
        private readonly ExtractorModel _model;

        /// <summary>
        /// Initializes a new instance of the <see cref="PosePipeline"/> class.
        /// </summary>
        /// <param name="poseModel">The pose model.</param>
        public PosePipeline(ExtractorModel poseModel)
        {
            _model = poseModel;
        }


        /// <summary>
        /// Loads the pipeline.
        /// </summary>
        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            await _model.LoadAsync(cancellationToken: cancellationToken);
        }


        /// <summary>
        /// Unloads the pipeline.
        /// </summary>
        public async Task UnloadAsync(CancellationToken cancellationToken = default)
        {
            await _model.UnloadAsync();
        }


        /// <summary>
        /// Run the Pose image pipeline with the specified PoseImageOptions
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="inputImage">The input image.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <param name="cancellationToken">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A Task&lt;ImageTensor&gt; representing the asynchronous operation.</returns>
        public async Task<ImageTensor> RunAsync(PoseImageOptions options, IProgress<RunProgress> progressCallback = null, CancellationToken cancellationToken = default)
        {
            var timestamp = RunProgress.GetTimestamp();
            var resultTensor = await ExtractPoseInternalAsync(options.Image, options, cancellationToken);
            progressCallback?.Report(new RunProgress(timestamp));
            return resultTensor;
        }


        /// <summary>
        /// Run the Pose video pipeline with the specified PoseVideoOptions
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <param name="cancellationToken">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A Task&lt;VideoTensor&gt; representing the asynchronous operation.</returns>
        public async Task<VideoTensor> RunAsync(PoseVideoOptions options, IProgress<RunProgress> progressCallback = null, CancellationToken cancellationToken = default)
        {
            var timestamp = RunProgress.GetTimestamp();
            var results = new List<ImageTensor>();
            foreach (var frame in options.Video.GetFrames())
            {
                var frameTime = Stopwatch.GetTimestamp();
                var resultTensor = await ExtractPoseInternalAsync(frame, options, cancellationToken);
                results.Add(resultTensor);
                progressCallback?.Report(new RunProgress(results.Count, options.Video.Frames, frameTime));
            }

            var resultVideoTensor = new VideoTensor(results.Join(), options.Video.FrameRate);
            progressCallback?.Report(new RunProgress(timestamp));
            return resultVideoTensor;
        }


        /// <summary>
        /// Run the Pose stream pipeline with the specified PoseStreamOptions
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <param name="cancellationToken">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A Task&lt;IAsyncEnumerable`1&gt; representing the asynchronous operation.</returns>
        public async IAsyncEnumerable<VideoFrame> RunAsync(PoseStreamOptions options, IProgress<RunProgress> progressCallback = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var frameCount = 0;
            var timestamp = RunProgress.GetTimestamp();
            await foreach (var videoFrame in options.Stream)
            {
                var frameTime = Stopwatch.GetTimestamp();
                var resultTensor = await ExtractPoseInternalAsync(videoFrame.Frame, options, cancellationToken);
                progressCallback?.Report(new RunProgress(++frameCount, 0, frameTime));
                yield return new VideoFrame(videoFrame.Index, resultTensor, videoFrame.SourceFrameRate);
            }
            progressCallback?.Report(new RunProgress(timestamp));
        }



        /// <summary>
        /// Disposes this pipeline.
        /// </summary>
        public void Dispose()
        {
            _model.Dispose();
        }


        /// <summary>
        /// Run Extract Pose on input ImageTensor
        /// </summary>
        /// <param name="imageInput">The image tensor.</param>
        /// <param name="cancellationToken">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        private async Task<ImageTensor> ExtractPoseInternalAsync(ImageTensor imageInput, PoseOptions options, CancellationToken cancellationToken = default)
        {
            var metadata = await _model.LoadAsync(cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var inputTensor = CLIPImage.Process(imageInput, _model.SampleSize, _model.SampleSize, TensorStack.Common.ResizeMode.LetterBox);
            using (var modelParameters = new ModelParameters(metadata, cancellationToken))
            {
                modelParameters.AddInput(inputTensor.GetChannels(_model.Channels));
                modelParameters.AddOutput();
                modelParameters.AddOutput();

                using (var results = _model.RunInference(modelParameters))
                {
                    var detections = results[0].ToTensor();
                    var keypoints = results[1].ToTensor();
                    var detectedPoses = DetectPose(detections, keypoints);
                    var imageTensor = BuildSkeleton(options, detectedPoses);
                    return imageTensor.ResizeImage(imageInput.Width, imageInput.Height, TensorStack.Common.ResizeMode.Crop);
                }
            }
        }


        /// <summary>
        /// Detects the pose.
        /// </summary>
        /// <param name="detections">The detections.</param>
        /// <param name="keypoints">The keypoints.</param>
        /// <returns>Detection[].</returns>
        private Detection[] DetectPose(Tensor<float> detections, Tensor<float> keypoints)
        {
            var numDets = detections.Dimensions[1];
            var numKeypoints = keypoints.Dimensions[2];
            var results = new Detection[numDets];
            for (int d = 0; d < numDets; d++)
            {
                var detOffset = (0 * numDets + d) * 5;
                var spanDet = detections.Memory.Span.Slice(detOffset, 5);

                var kpts = new List<Keypoint>();
                int kptOffset = ((0 * numDets) + d) * numKeypoints * 3;
                for (int k = 0; k < numKeypoints; k++)
                {
                    int baseIdx = kptOffset + k * 3;
                    var spanK = keypoints.Memory.Span.Slice(baseIdx, 3);
                    kpts.Add(new Keypoint(spanK[0], spanK[1], spanK[2]));
                }

                float x1 = spanDet[0];
                float y1 = spanDet[1];
                float x2 = spanDet[2];
                float y2 = spanDet[3];
                float conf = spanDet[4];

                // Compute Neck
                var leftShoulder = kpts[5];
                var rightShoulder = kpts[6];
                var neck = new Keypoint((leftShoulder.X + rightShoulder.X) / 2f, (leftShoulder.Y + rightShoulder.Y) / 2f, MathF.Min(leftShoulder.Confidence, rightShoulder.Confidence));
                kpts.Insert(1, neck);
                results[d] = new Detection(x1, y1, x2, y2, conf, kpts);
            }
            return results;
        }


        /// <summary>
        /// Builds the skeleton.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="detections">The detections.</param>
        /// <returns>ImageTensor.</returns>
        private ImageTensor BuildSkeleton(PoseOptions options, Detection[] detections)
        {
            using (var bitmap = new SKBitmap(_model.SampleSize, _model.SampleSize, SKColorType.Rgba8888, SKAlphaType.Premul))
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(options.IsTransparent
                    ? new SKColor(0, 0, 0, 0)
                    : new SKColor(0, 0, 0, 255));

                var selectedDetections = options.Detections > 0
                    ? detections.Take(options.Detections)
                    : detections;

                foreach (var detection in selectedDetections)
                {
                    if (detection.Confidence < options.BodyConfidence)
                        continue;

                    foreach (var keypoint in detection.Keypoints)
                    {
                        keypoint.IsValid = keypoint.Confidence > options.JointConfidence;
                    }
                    DrawSkeleton(canvas, options, detection.Keypoints);
                }
                return CreateTensor(bitmap);
            }
        }


        /// <summary>
        /// Draws the skeleton.
        /// </summary>
        /// <param name="imageContext">The CTX.</param>
        /// <param name="options">The options.</param>
        /// <param name="keypoints">The keypoints.</param>
        private static void DrawSkeleton(SKCanvas canvas, PoseOptions options, IReadOnlyList<Keypoint> keypoints)
        {
            if (keypoints == null || keypoints.Count < 17)
                return;

            foreach (var bone in BoneMap)
            {
                var a = keypoints[bone.JointA];
                var b = keypoints[bone.JointB];
                if (!a.IsValid || !b.IsValid)
                    continue;

                DrawBone(canvas, a, b, bone.Color, options.BoneThickness, options.BoneRadius);
            }

            foreach (var joint in JointMap)
            {
                var k = keypoints[joint.Id];
                if (!k.IsValid)
                    continue;

                DrawJoint(canvas, k, joint.Color, options.JointRadius);
            }
        }


        /// <summary>
        /// Draws the joint.
        /// </summary>
        /// <param name="imageContext">The image context.</param>
        /// <param name="keypoint">The keypoint.</param>
        /// <param name="color">The color.</param>
        /// <param name="circleRadius">The circle radius.</param>
        private static void DrawJoint(SKCanvas canvas, Keypoint keypoint, SKColor color, float radius)
        {
            using var paint = new SKPaint
            {
                Color = color,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            canvas.DrawCircle(keypoint.X, keypoint.Y, radius, paint);
        }


        /// <summary>
        /// Draws the bone.
        /// </summary>
        /// <param name="imageContext">The image context.</param>
        /// <param name="jointA">a.</param>
        /// <param name="jointB">The b.</param>
        /// <param name="color">The color.</param>
        /// <param name="thickness">The thickness.</param>
        /// <param name="curvature">The curvature.</param>
        private static void DrawBone(SKCanvas canvas, Keypoint jointA, Keypoint jointB, SKColor color, float thickness, float curvature)
        {
            var direction = new SKPoint(jointB.X - jointA.X, jointB.Y - jointA.Y);
            var length = MathF.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
            if (length < 1f)
                return;

            var curveOffset = thickness * curvature;
            direction = new SKPoint(direction.X / length, direction.Y / length);
            var perp = new SKPoint(-direction.Y, direction.X);
            using (var paint = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill })
            using (var path = new SKPath())
            {
                // left side
                var p0 = new SKPoint(jointA.X + perp.X * thickness, jointA.Y + perp.Y * thickness);
                var p1 = new SKPoint(jointA.X + perp.X * (thickness + curveOffset), jointA.Y + perp.Y * (thickness + curveOffset));
                var p2 = new SKPoint(jointB.X + perp.X * (thickness + curveOffset), jointB.Y + perp.Y * (thickness + curveOffset));
                var p3 = new SKPoint(jointB.X + perp.X * thickness, jointB.Y + perp.Y * thickness);

                // right side
                var q0 = new SKPoint(jointB.X - perp.X * thickness, jointB.Y - perp.Y * thickness);
                var q1 = new SKPoint(jointB.X - perp.X * (thickness + curveOffset), jointB.Y - perp.Y * (thickness + curveOffset));
                var q2 = new SKPoint(jointA.X - perp.X * (thickness + curveOffset), jointA.Y - perp.Y * (thickness + curveOffset));
                var q3 = new SKPoint(jointA.X - perp.X * thickness, jointA.Y - perp.Y * thickness);

                path.MoveTo(p0);
                path.CubicTo(p1, p2, p3);
                path.CubicTo(q1, q2, q3);
                path.Close();
                canvas.DrawPath(path, paint);
            }
        }


        /// <summary>
        /// Creates the tensor.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <returns>ImageTensor.</returns>
        private static ImageTensor CreateTensor(SKBitmap bitmap)
        {
            var tensor = new Tensor<float>([1, 4, bitmap.Height, bitmap.Width]);
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var c = bitmap.GetPixel(x, y);
                    tensor[0, 0, y, x] = GetFloatValue(c.Red);
                    tensor[0, 1, y, x] = GetFloatValue(c.Green);
                    tensor[0, 2, y, x] = GetFloatValue(c.Blue);
                    tensor[0, 3, y, x] = GetFloatValue(c.Alpha);
                }
            }
            return tensor.AsImageTensor();
        }


        /// <summary>
        /// Gets the float value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Single.</returns>
        private static float GetFloatValue(byte value)
        {
            return (value / 255.0f) * 2.0f - 1.0f;
        }


        private record Bone(int JointA, int JointB, SKColor Color);
        private record Joint(int Id, SKColor Color);
        private record Keypoint(float X, float Y, float Confidence) { public bool IsValid { get; set; } }
        private record Detection(float X1, float Y1, float X2, float Y2, float Confidence, IReadOnlyList<Keypoint> Keypoints);

        private static readonly Bone[] BoneMap =
        {
            new Bone(0, 1,   new SKColor(0, 0, 153)),   // nose -> neck
            new Bone(0, 2,  new SKColor(153, 0, 153)), // nose -> left eye
            new Bone(2, 4,  new SKColor(153, 0, 102)), // left eye -> left ear
            new Bone(0, 3,  new SKColor(51, 0, 153)),  // nose -> right eye
            new Bone(3, 5,  new SKColor(102, 0, 153)), // right eye -> right ear
            new Bone(1, 6,  new SKColor(153, 51, 0)),  // neck -> left sholder
            new Bone(1, 7,  new SKColor(153, 0, 0)),   // neck -> right sholder
            new Bone(6, 8,  new SKColor(102, 153, 0)), // left sholder -> left arm
            new Bone(7, 9,  new SKColor(153, 102, 0)), // right sholder -> right arm
            new Bone(8, 10, new SKColor(51, 153, 0)),  // left arm -> left wrist
            new Bone(9, 11, new SKColor(153, 153, 0)), // right arm -> right wrist
            new Bone(1, 12, new SKColor(0, 153, 153)), // neck -> left hip
            new Bone(1, 13, new SKColor(0, 153, 0)),   // neck -> right hip
            new Bone(12, 14,new SKColor(0, 102, 153)), // left hip -> left knee
            new Bone(13, 15,new SKColor(0, 153, 51)),  // right hip -> right knee
            new Bone(14, 16,new SKColor(0, 51, 153)),  // left knee -> left ankle
            new Bone(15, 17,new SKColor(0, 153, 102)), // right knee -> right ankle
        };


        private static readonly Joint[] JointMap =
        [
            new Joint(0, new SKColor(255,0,0)),     // nose
            new Joint(1, new SKColor(255,85,0)),    // neck
            new Joint(2, new SKColor(255, 0, 255)), // left_eye
            new Joint(3, new SKColor(170, 0, 255)), // right_eye
            new Joint(4, new SKColor(255, 0, 85)),  // left_ear
            new Joint(5, new SKColor(255, 0, 170)), // right_ear
            new Joint(6, new SKColor(85, 255, 0)),  // left_shoulder
            new Joint(7, new SKColor(255, 170, 0)), // right_shoulder
            new Joint(8, new SKColor(0, 255, 0)),   // left_elbow
            new Joint(9, new SKColor(255, 255, 0)), // right_elbow
            new Joint(10,new SKColor(0, 255, 85)),  // left_wrist
            new Joint(11,new SKColor(170, 255, 0)), // right_wrist
            new Joint(12,new SKColor(0, 85, 255)),  // left_hip
            new Joint(13,new SKColor(0, 255, 170)), // right_hip
            new Joint(14,new SKColor(0, 0, 255)),   // left_knee
            new Joint(15,new SKColor(0, 255, 255)), // right_knee
            new Joint(16,new SKColor(85, 0, 255)),  // left_ankle
            new Joint(17,new SKColor(0, 170, 255))  // right_ankle
        ];


        /// <summary>
        /// Creates an PosePipeline
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>PosePipeline.</returns>
        public static PosePipeline Create(ExtractorConfig configuration)
        {
            var extractorModel = ExtractorModel.Create(configuration);
            return new PosePipeline(extractorModel);
        }

    }
}
