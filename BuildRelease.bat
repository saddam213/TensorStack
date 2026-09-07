dotnet build TensorStack.Common/TensorStack.Common.csproj -c Release
dotnet pack TensorStack.Common/TensorStack.Common.csproj -c Release

dotnet build TensorStack.Media/TensorStack.Media.csproj -c Release
dotnet pack TensorStack.Media/TensorStack.Media.csproj -c Release

dotnet build TensorStack.Media.BitmapImage/TensorStack.Media.BitmapImage.csproj -c Release
dotnet pack TensorStack.Media.BitmapImage/TensorStack.Media.BitmapImage.csproj -c Release

dotnet build TensorStack.Media.SkiaSharp/TensorStack.Media.SkiaSharp.csproj -c Release
dotnet pack TensorStack.Media.SkiaSharp/TensorStack.Media.SkiaSharp.csproj -c Release

dotnet build TensorStack.OnnxRuntime/TensorStack.OnnxRuntime.csproj -c Release
dotnet pack TensorStack.OnnxRuntime/TensorStack.OnnxRuntime.csproj -c Release

dotnet build TensorStack.OnnxRuntime.LLM/TensorStack.OnnxRuntime.LLM.csproj -c Release
dotnet pack TensorStack.OnnxRuntime.LLM/TensorStack.OnnxRuntime.LLM.csproj -c Release

dotnet build TensorStack.OnnxRuntime.Vision/TensorStack.OnnxRuntime.Vision.csproj -c Release
dotnet pack TensorStack.OnnxRuntime.Vision/TensorStack.OnnxRuntime.Vision.csproj -c Release

dotnet build TensorStack.HuggingFace/TensorStack.HuggingFace.csproj -c Release
dotnet pack TensorStack.HuggingFace/TensorStack.HuggingFace.csproj -c Release

dotnet build TensorStack.StableDiffusionCpp/TensorStack.StableDiffusionCpp.csproj -c Release
dotnet pack TensorStack.StableDiffusionCpp/TensorStack.StableDiffusionCpp.csproj -c Release

dotnet build TensorStack.WPF/TensorStack.WPF.csproj -c Release
dotnet pack TensorStack.WPF/TensorStack.WPF.csproj -c Release