dotnet build TensorStack.Common/TensorStack.Common.csproj -c Release
dotnet pack TensorStack.Common/TensorStack.Common.csproj -c Release

dotnet build TensorStack.Media/TensorStack.Media.csproj -c Release
dotnet pack TensorStack.Media/TensorStack.Media.csproj -c Release

dotnet build TensorStack.Media.Windows/TensorStack.Media.Windows.csproj -c Release
dotnet pack TensorStack.Media.Windows/TensorStack.Media.Windows.csproj -c Release

dotnet build TensorStack.Media.Bitmap/TensorStack.Media.Bitmap.csproj -c Release
dotnet pack TensorStack.Media.Bitmap/TensorStack.Media.Bitmap.csproj -c Release

dotnet build TensorStack.Media.BitmapImage/TensorStack.Media.BitmapImage.csproj -c Release
dotnet pack TensorStack.Media.BitmapImage/TensorStack.Media.BitmapImage.csproj -c Release

dotnet build TensorStack.Media.SkiaSharp/TensorStack.Media.SkiaSharp.csproj -c Release
dotnet pack TensorStack.Media.SkiaSharp/TensorStack.Media.SkiaSharp.csproj -c Release

dotnet build TensorStack.Providers.CPU/TensorStack.Providers.CPU.csproj -c Release
dotnet pack TensorStack.Providers.CPU/TensorStack.Providers.CPU.csproj -c Release

dotnet build TensorStack.Providers.CUDA/TensorStack.Providers.CUDA.csproj -c Release
dotnet pack TensorStack.Providers.CUDA/TensorStack.Providers.CUDA.csproj -c Release

dotnet build TensorStack.Providers.DML/TensorStack.Providers.DML.csproj -c Release
dotnet pack TensorStack.Providers.DML/TensorStack.Providers.DML.csproj -c Release

dotnet build TensorStack.Extractors/TensorStack.Extractors.csproj -c Release
dotnet pack TensorStack.Extractors/TensorStack.Extractors.csproj -c Release

dotnet build TensorStack.Upscaler/TensorStack.Upscaler.csproj -c Release
dotnet pack TensorStack.Upscaler/TensorStack.Upscaler.csproj -c Release

dotnet build TensorStack.TextGeneration/TensorStack.TextGeneration.csproj -c Release
dotnet pack TensorStack.TextGeneration/TensorStack.TextGeneration.csproj -c Release

dotnet build TensorStack.Python/TensorStack.Python.csproj -c Release
dotnet pack TensorStack.Python/TensorStack.Python.csproj -c Release

dotnet build TensorStack.WPF/TensorStack.WPF.csproj -c Release
dotnet pack TensorStack.WPF/TensorStack.WPF.csproj -c Release