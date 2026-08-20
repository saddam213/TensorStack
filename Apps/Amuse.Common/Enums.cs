using System.ComponentModel.DataAnnotations;

namespace Amuse.Common
{
    public enum ServerType
    {
        OnnxRuntime = 0,
        PyTorch = 10,
        StableDiffusionCpp = 20
    }

    public enum ProcessType
    {
        // Image
        [Display(Name = "TextToImage", ShortName = "T2I", Description = "Generates a brand new synthetic image completely from scratch based on a text prompt.")]
        TextToImage = 0,

        [Display(Name = "ImageToImage", ShortName = "I2I", Description = "Alters a source image changing its style, textures, or composition based on a guiding text prompt.")]
        ImageToImage = 1,

        [Display(Name = "ImageEdit", ShortName = "IE", Description = "Applies localized or structural modifications to an existing image using instruction-based editing commands.")]
        ImageEdit = 2,

        [Display(Name = "ImageInpaint", ShortName = "INP", Description = "Modifies or restores specific, masked areas within an image while preserving the surrounding context.")]
        ImageInpaint = 3,

        [Display(Name = "ImageControlNet", ShortName = "CN", Description = "Applies rigid spatial conditioning (like edge maps, poses, or depth) onto a text-to-image generation process.")]
        ImageControlNet = 4,

        [Display(Name = "ImageToImageControlNet", ShortName = "I2I+CN", Description = "Combines a source image with an explicit spatial guide map to tightly control composition and style concurrently.")]
        ImageToImageControlNet = 5,


        // Video
        [Display(Name = "TextToVideo", ShortName = "T2V", Description = "Synthesizes fluid, moving video frames from scratch using a conceptual text prompt.")]
        TextToVideo = 300,

        [Display(Name = "ImageToVideo", ShortName = "I2V", Description = "Animates a single, static source image into a moving video clip while maintaining character or object consistency.")]
        ImageToVideo = 301,

        [Display(Name = "VideoToVideo", ShortName = "V2V", Description = "Translates a source video into a different style or texture while tracking the underlying motion structures.")]
        VideoToVideo = 302,


        // Audio
        [Display(Name = "TextToAudio", ShortName = "T2A", Description = "Converts written text into spoken voice synthesis, realistic sound effects, or continuous music tracks.")]
        TextToAudio = 400,

        [Display(Name = "AudioToText", ShortName = "A2T", Description = "Transcribes incoming spoken speech or environmental audio signals into formatted, written text.")]
        AudioToText = 500,


        // Text
        [Display(Name = "TextToText", ShortName = "T2T", Description = "")]
        TextToText = 800,

        [Display(Name = "ImageToText", ShortName = "I2T", Description = "")]
        ImageToText = 801
    }

    public enum EnvironmentMode
    {
        Create = 0,
        Load = 1,
        Update = 2,
        Rebuild = 3,
        Reinstall = 4
    }

    public enum DataType
    {
        Float32 = 0,
        Bfloat16 = 1,
        Float16 = 2,
        Float8 = 3,
        Int8 = 6,
        Int4 = 7
    }

    public enum QuantizationType
    {
        Q16Bit = 0,
        Q8Bit = 1,
        Q4Bit = 2
    }

    public enum MemoryModeType
    {
        Device = 0,
        OffloadCPU = 1,
        OffloadModel = 2,
        Balanced = 3
    }

    public enum SchedulerType
    {
        [Display(Name = "LMS")]
        LMS = 0,

        [Display(Name = "Euler")]
        Euler = 1,

        [Display(Name = "Euler Ancestral")]
        EulerAncestral = 2,

        [Display(Name = "DDPM")]
        DDPM = 3,

        [Display(Name = "DDIM")]
        DDIM = 4,

        [Display(Name = "KDPM2")]
        KDPM2 = 5,

        [Display(Name = "KDPM2-Ancestral")]
        KDPM2Ancestral = 6,

        [Display(Name = "DDPM-Wuerstchen")]
        DDPMWuerstchen = 10,

        [Display(Name = "LCM")]
        LCM = 20,

        [Display(Name = "FlowMatch-Euler")]
        FlowMatchEuler = 30,

        [Display(Name = "FlowMatch-Heun")]
        FlowMatchHeun = 31,

        [Display(Name = "PNDM")]
        PNDM = 40,

        [Display(Name = "Heun")]
        Heun = 41,

        [Display(Name = "UniPC Multistep")]
        UniPCMultistep = 42,

        [Display(Name = "DPM Solver Multistep")]
        DPMSolverMultistep = 43,

        [Display(Name = "DPM Single Step")]
        DPMSolverSinglestep = 45,

        [Display(Name = "DPM Solver SDE")]
        DPMSolverSDE = 46,

        [Display(Name = "DEIS Multistep")]
        DEISMultistep = 47,

        [Display(Name = "EDM Euler")]
        EDMEuler = 48,

        [Display(Name = "EDM DPM Solver Multistep")]
        EDMDPMSolverMultistep = 49,

        [Display(Name = "FlowMatch-LCM")]
        FlowMatchLCM = 50,

        [Display(Name = "IPNDM")]
        IPNDM = 51,

        [Display(Name = "CogVideoX DDIM")]
        CogVideoXDDIM = 52,

        [Display(Name = "CogVideoX DPM")]
        CogVideoXDPM = 53,

        [Display(Name = "Helios")]
        Helios = 54,

        [Display(Name = "Helios DMD")]
        HeliosDMD = 55,

        [Display(Name = "TCD")]
        TCD = 56,

        [Display(Name = "SCM")]
        SCM = 57,

        [Display(Name = "SA Solver")]
        SASolver = 58,

        [Display(Name = "LTX Euler Ancestral")]
        LTXEulerAncestral = 59,

        [Display(Name = "DPM2")]
        DPM2 = 60,

        [Display(Name = "DPM++ 2S Ancestral")]
        DPMPlusPlus2SAncestral = 61,

        [Display(Name = "DPM++ 2M")]
        DPMPlusPlus2M = 62,

        [Display(Name = "DPM++ 2M v2")]
        DPMPlusPlus2Mv2 = 63,

        [Display(Name = "DPM++ 2M SDE")]
        DPMPlusPlus2MSDE = 64,

        [Display(Name = "DPM++ 2M SDE BT")]
        DPMPlusPlus2MSDEBT = 65,

        [Display(Name = "Residual Multistep")]
        ResidualMultistep = 66,

        [Display(Name = "Residual 2S")]
        Residual2S = 67,

        [Display(Name = "ER-SDE")]
        ERSDE = 68,
    }


    public enum SigmaScheduleType
    {
        [Display(Name = "Default")]
        Default = 0,

        [Display(Name = "Discrete")]
        Discrete = 1,

        [Display(Name = "Normal")]
        Normal = 2,

        [Display(Name = "Karras")]
        Karras = 3,

        [Display(Name = "Exponential")]
        Exponential = 4,

        [Display(Name = "AYS")]
        AYS = 5,

        [Display(Name = "GITS")]
        GITS = 6,

        [Display(Name = "SGMUniform")]
        SGMUniform = 7,

        [Display(Name = "Simple")]
        Simple = 8,

        [Display(Name = "Smoothstep")]
        Smoothstep = 9,

        [Display(Name = "KLOptimal")]
        KLOptimal = 10,

        [Display(Name = "LCM")]
        LCM = 11,

        [Display(Name = "BongTangent")]
        BongTangent = 12,

        [Display(Name = "LTX2")]
        LTX2 = 13,

        [Display(Name = "LogitNormal")]
        LogitNormal = 14,

        [Display(Name = "Flux")]
        Flux = 15,

        [Display(Name = "Flux2")]
        Flux2 = 16,

        [Display(Name = "Beta")]
        Beta = 17,
    }

    public enum TimestepSpacingType
    {
        Leading = 0,
        Trailing = 1,
        Linspace = 2
    }

    public enum AlgorithmType
    {
        DPMSolver = 0,
        DPMSolverPlus = 1,
        SDE_DPMSolver = 2,
        SDE_DPMSolverPlus = 3,
        DEIS = 4,
        DataPrediction = 5,
        NoisePrediction = 6
    }

    public enum SolverType
    {
        Midpoint = 0,
        Heun = 1,
        BH1 = 2,
        BH2 = 3,
        LogRho = 4
    }

    public enum BetaScheduleType
    {
        Linear = 0,
        ScaledLinear = 1,
        Cosine = 2,
        SquaredCosine = 3,
        Sigmoid = 4,
        Laplace = 5,
        Exponential = 6
    }

    public enum PredictionType
    {
        Epsilon = 0,
        Variable = 1,
        Sample = 2,
        FlowPrediction = 3,
        Trigflow = 4
    }

    public enum VarianceType
    {
        FixedSmall = 0,
        FixedSmallLog = 1,
        FixedLarge = 2,
        FixedLargeLog = 3,
        Learned = 4,
        LearnedRange = 5
    }

    public enum TimeShiftType
    {
        Linear = 0,
        Exponential = 1
    }

    public enum AlphaTransformType
    {
        Cosine = 0,
        Exponential = 1,
        Laplace = 2
    }

    public enum FinalSigmasType
    {
        Zero = 0,
        SigmaMin = 1
    }

    public enum InterpolationType
    {
        Linear = 0,
        LogLinear = 1
    }

    public enum TimestepType
    {
        Discrete = 0,
        Continuous = 1
    }

    public enum UpscaleModeType
    {
        Nearest = 0,
        Linear = 1,
        Bilinear = 2,
        Bicubic = 3,
        Trilinear = 4,
        Area = 5,
        NearestExact = 6
    }


    public enum LanguageType
    {
        [Display(Name = "None", ShortName = "")]
        None = 0,

        [Display(Name = "Auto", ShortName = "en")]
        Auto = 1,

        [Display(Name = "Afrikaans", ShortName = "af")]
        Afrikaans = 2,

        [Display(Name = "Albanian", ShortName = "sq")]
        Albanian = 3,

        [Display(Name = "Amharic", ShortName = "am")]
        Amharic = 4,

        [Display(Name = "Arabic", ShortName = "ar")]
        Arabic = 5,

        [Display(Name = "Armenian", ShortName = "hy")]
        Armenian = 6,

        [Display(Name = "Assamese", ShortName = "as")]
        Assamese = 7,

        [Display(Name = "Azerbaijani", ShortName = "az")]
        Azerbaijani = 8,

        [Display(Name = "Bashkir", ShortName = "ba")]
        Bashkir = 9,

        [Display(Name = "Basque", ShortName = "eu")]
        Basque = 10,

        [Display(Name = "Belarusian", ShortName = "be")]
        Belarusian = 11,

        [Display(Name = "Bengali", ShortName = "bn")]
        Bengali = 12,

        [Display(Name = "Bosnian", ShortName = "bs")]
        Bosnian = 13,

        [Display(Name = "Breton", ShortName = "br")]
        Breton = 14,

        [Display(Name = "Bulgarian", ShortName = "bg")]
        Bulgarian = 15,

        [Display(Name = "Burmese", ShortName = "my")]
        Burmese = 16,

        [Display(Name = "Catalan", ShortName = "ca")]
        Catalan = 17,

        [Display(Name = "Chinese", ShortName = "zh")]
        Chinese = 18,

        [Display(Name = "Croatian", ShortName = "hr")]
        Croatian = 19,

        [Display(Name = "Czech", ShortName = "cs")]
        Czech = 20,

        [Display(Name = "Danish", ShortName = "da")]
        Danish = 21,

        [Display(Name = "Dutch", ShortName = "nl")]
        Dutch = 22,

        [Display(Name = "English", ShortName = "en")]
        English = 23,

        [Display(Name = "Estonian", ShortName = "et")]
        Estonian = 24,

        [Display(Name = "Faroese", ShortName = "fo")]
        Faroese = 25,

        [Display(Name = "Finnish", ShortName = "fi")]
        Finnish = 26,

        [Display(Name = "French", ShortName = "fr")]
        French = 27,

        [Display(Name = "Galician", ShortName = "gl")]
        Galician = 28,

        [Display(Name = "Georgian", ShortName = "ka")]
        Georgian = 29,

        [Display(Name = "German", ShortName = "de")]
        German = 30,

        [Display(Name = "Greek", ShortName = "el")]
        Greek = 31,

        [Display(Name = "Gujarati", ShortName = "gu")]
        Gujarati = 32,

        [Display(Name = "Haitian", ShortName = "ht")]
        Haitian = 33,

        [Display(Name = "Hausa", ShortName = "ha")]
        Hausa = 34,

        [Display(Name = "Hawaiian", ShortName = "haw")]
        Hawaiian = 35,

        [Display(Name = "Hebrew", ShortName = "he")]
        Hebrew = 36,

        [Display(Name = "Hindi", ShortName = "hi")]
        Hindi = 37,

        [Display(Name = "Hungarian", ShortName = "hu")]
        Hungarian = 38,

        [Display(Name = "Icelandic", ShortName = "is")]
        Icelandic = 39,

        [Display(Name = "Indonesian", ShortName = "id")]
        Indonesian = 40,

        [Display(Name = "Italian", ShortName = "it")]
        Italian = 41,

        [Display(Name = "Japanese", ShortName = "ja")]
        Japanese = 42,

        [Display(Name = "Javanese", ShortName = "jw")]
        Javanese = 43,

        [Display(Name = "Kannada", ShortName = "kn")]
        Kannada = 44,

        [Display(Name = "Kazakh", ShortName = "kk")]
        Kazakh = 45,

        [Display(Name = "Khmer", ShortName = "km")]
        Khmer = 46,

        [Display(Name = "Korean", ShortName = "ko")]
        Korean = 47,

        [Display(Name = "Lao", ShortName = "lo")]
        Lao = 48,

        [Display(Name = "Latin", ShortName = "la")]
        Latin = 49,

        [Display(Name = "Latvian", ShortName = "lv")]
        Latvian = 50,

        [Display(Name = "Lingala", ShortName = "ln")]
        Lingala = 51,

        [Display(Name = "Lithuanian", ShortName = "lt")]
        Lithuanian = 52,

        [Display(Name = "Luxembourgish", ShortName = "lb")]
        Luxembourgish = 53,

        [Display(Name = "Macedonian", ShortName = "mk")]
        Macedonian = 54,

        [Display(Name = "Malagasy", ShortName = "mg")]
        Malagasy = 55,

        [Display(Name = "Malay", ShortName = "ms")]
        Malay = 56,

        [Display(Name = "Malayalam", ShortName = "ml")]
        Malayalam = 57,

        [Display(Name = "Maltese", ShortName = "mt")]
        Maltese = 58,

        [Display(Name = "Maori", ShortName = "mi")]
        Maori = 59,

        [Display(Name = "Marathi", ShortName = "mr")]
        Marathi = 60,

        [Display(Name = "Mongolian", ShortName = "mn")]
        Mongolian = 61,

        [Display(Name = "Nepali", ShortName = "ne")]
        Nepali = 62,

        [Display(Name = "Norwegian", ShortName = "no")]
        Norwegian = 63,

        [Display(Name = "Norwegian Nynorsk", ShortName = "nn")]
        NorwegianNynorsk = 64,

        [Display(Name = "Occitan", ShortName = "oc")]
        Occitan = 65,

        [Display(Name = "Persian", ShortName = "fa")]
        Persian = 66,

        [Display(Name = "Polish", ShortName = "pl")]
        Polish = 67,

        [Display(Name = "Portuguese", ShortName = "pt")]
        Portuguese = 68,

        [Display(Name = "Punjabi", ShortName = "pa")]
        Punjabi = 69,

        [Display(Name = "Romanian", ShortName = "ro")]
        Romanian = 70,

        [Display(Name = "Russian", ShortName = "ru")]
        Russian = 71,

        [Display(Name = "Sanskrit", ShortName = "sa")]
        Sanskrit = 72,

        [Display(Name = "Serbian", ShortName = "sr")]
        Serbian = 73,

        [Display(Name = "Shona", ShortName = "sn")]
        Shona = 74,

        [Display(Name = "Sindhi", ShortName = "sd")]
        Sindhi = 75,

        [Display(Name = "Sinhala", ShortName = "si")]
        Sinhala = 76,

        [Display(Name = "Slovak", ShortName = "sk")]
        Slovak = 77,

        [Display(Name = "Slovenian", ShortName = "sl")]
        Slovenian = 78,

        [Display(Name = "Somali", ShortName = "so")]
        Somali = 79,

        [Display(Name = "Spanish", ShortName = "es")]
        Spanish = 80,

        [Display(Name = "Sundanese", ShortName = "su")]
        Sundanese = 81,

        [Display(Name = "Swahili", ShortName = "sw")]
        Swahili = 82,

        [Display(Name = "Swedish", ShortName = "sv")]
        Swedish = 83,

        [Display(Name = "Tagalog", ShortName = "tl")]
        Tagalog = 84,

        [Display(Name = "Tajik", ShortName = "tg")]
        Tajik = 85,

        [Display(Name = "Tamil", ShortName = "ta")]
        Tamil = 86,

        [Display(Name = "Tatar", ShortName = "tt")]
        Tatar = 87,

        [Display(Name = "Telugu", ShortName = "te")]
        Telugu = 88,

        [Display(Name = "Thai", ShortName = "th")]
        Thai = 89,

        [Display(Name = "Tibetan", ShortName = "bo")]
        Tibetan = 90,

        [Display(Name = "Turkish", ShortName = "tr")]
        Turkish = 91,

        [Display(Name = "Turkmen", ShortName = "tk")]
        Turkmen = 92,

        [Display(Name = "Ukrainian", ShortName = "uk")]
        Ukrainian = 93,

        [Display(Name = "Urdu", ShortName = "ur")]
        Urdu = 94,

        [Display(Name = "Uzbek", ShortName = "uz")]
        Uzbek = 95,

        [Display(Name = "Vietnamese", ShortName = "vi")]
        Vietnamese = 96,

        [Display(Name = "Welsh", ShortName = "cy")]
        Welsh = 97,

        [Display(Name = "Yiddish", ShortName = "yi")]
        Yiddish = 98,

        [Display(Name = "Yoruba", ShortName = "yo")]
        Yoruba = 99
    }


    public enum ConversationRole
    {
        User = 0,
        System = 1,
        Assistant = 2
    }


    public enum CacheType
    {
        Dynamic = 0,
        DynamicOffload = 1,
        Static = 2,
        StaticOffload = 3,
        Quantized = 4,
        Disabled = 100,
    }


    public enum LatentUpscale
    {
        [Display(Name = "None")]
        None,

        [Display(Name = "Model")]
        Model,

        [Display(Name = "Latent")]
        Latent,

        [Display(Name = "Latent (nearest)")]
        LatentNearest,

        [Display(Name = "Latent (nearest-exact)")]
        LatentNearestExact,

        [Display(Name = "Latent (antialiased)")]
        LatentAntialiased,

        [Display(Name = "Latent (bicubic)")]
        LatentBicubic,

        [Display(Name = "Latent (bicubic antialiased)")]
        LatentBicubicAntialiased,

        [Display(Name = "Lanczos")]
        Lanczos,

        [Display(Name = "Nearest")]
        Nearest
    }
}
