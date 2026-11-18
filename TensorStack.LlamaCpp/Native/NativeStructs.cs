using System;
using System.Runtime.InteropServices;

namespace TensorStack.LlamaCpp.Native
{
    internal static unsafe partial class NativeApi
    {
        internal enum DeviceType
        {
            Cpu = 0,
            Gpu = 1,
            IntegratedGpu = 2,
            Accelerator = 3,
            Meta = 4
        }


        internal enum GgmlType
        {
            F32 = 0,
            F16 = 1,
            Q4_0 = 2,
            Q8_0 = 8
        }


        internal enum FlashAttentionType
        {
            Auto = -1,
            Disabled = 0,
            Enabled = 1
        }


        internal enum ProcessType
        {
            Encode = 0,
            Decode = 1
        }


        internal enum ChunkType
        {
            Text = 0,
            Image = 1,
            Audio = 2
        }


        [Flags]
        internal enum TokenAttribute
        {
            Undefined = 0,
            Unknown = 1 << 0,
            Unused = 1 << 1,
            Normal = 1 << 2,
            Control = 1 << 3,
            UserDefined = 1 << 4,
            Byte = 1 << 5,
            Normalized = 1 << 6,
            LeftStrip = 1 << 7,
            RightStrip = 1 << 8,
            SingleWord = 1 << 9
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ModelParams
        {
            public nint* Devices;
            public nint TensorBufferOverrides;
            public int GpuLayers;
            public int SplitMode;
            public int LoadMode;
            public int LazyMode;
            public int MainGpu;
            public float* TensorSplit;
            public nint ProgressCallback;
            public nint ProgressCallbackUserData;
            public nint KeyValueOverrides;
            public byte VocabOnly;
            public byte CheckTensors;
            public byte UseExtraBufferTypes;
            public byte NoHost;
            public byte NoAlloc;
            public byte LoadMtp;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct ContextParams
        {
            public uint ContextSize;
            public uint BatchSize;
            public uint MicroBatchSize;
            public uint SequenceMaximum;
            public uint RecurrentStateSequences;
            public uint OutputsMaximum;
            public uint OutputsMaximumPerSequence;
            public int Threads;
            public int ThreadsBatch;
            public int ContextType;
            public int RopeScalingType;
            public int PoolingType;
            public int AttentionType;
            public FlashAttentionType FlashAttentionType;
            public float RopeFrequencyBase;
            public float RopeFrequencyScale;
            public float YarnExtFactor;
            public float YarnAttentionFactor;
            public float YarnBetaFast;
            public float YarnBetaSlow;
            public uint YarnOriginalContext;
            public nint EvalCallback;
            public nint EvalCallbackUserData;
            public GgmlType TypeK;
            public GgmlType TypeV;
            public nuint MoeCacheSize;
            public float MoeCacheCpuShare;
            public delegate* unmanaged[Cdecl]<void*, byte> AbortCallback;
            public void* AbortCallbackData;
            public byte Embeddings;
            public byte OffloadKqv;
            public byte NoPerformance;
            public byte OperationOffload;
            public byte SwaFull;
            public byte KeyValueUnified;
            public nint Samplers;
            public nuint SamplerCount;
            public nint ContextOther;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct SamplerChainParams
        {
            public byte NoPerformance;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct MtmdContextParams
        {
            public byte UseGpu;
            public nint Device;
            public byte PrintTimings;
            public int Threads;
            public byte* ImageMarker;
            public byte* MediaMarker;
            public FlashAttentionType FlashAttentionType;
            public byte Warmup;
            public int ImageMinTokens;
            public int ImageMaxTokens;
            public nint EvalCallback;
            public nint EvalCallbackUserData;
            public int BatchMaxTokens;
            public nint ProgressCallback;
            public nint ProgressCallbackUserData;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct MtmdInputText
        {
            public byte* Text;
            public nuint TextLength;
            public byte AddSpecial;
            public byte ParseSpecial;
        }
    }
}
