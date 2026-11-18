using System.ComponentModel.DataAnnotations;

namespace TensorStack.Common
{
    /// <summary>
    /// Tensor data types across all backends. Each backend translates the types it supports to its own native type.
    /// </summary>
    public enum DataType
    {
        [Display(Name = "Default", ShortName = "auto", Description = "Use the model or backend default")]
        Default = 0,

        // Float
        [Display(Name = "Float32", ShortName = "f32")]
        Float32 = 1,

        [Display(Name = "Float16", ShortName = "f16")]
        Float16 = 2,

        [Display(Name = "BFloat16", ShortName = "bf16")]
        BFloat16 = 3,

        [Display(Name = "Float8 E4M3", ShortName = "f8_e4m3")]
        Float8 = 4,

        [Display(Name = "Float8 E5M2", ShortName = "f8_e5m2")]
        Float8E5M2 = 5,

        [Display(Name = "Float64", ShortName = "f64")]
        Float64 = 6,

        // Integer
        [Display(Name = "Int8", ShortName = "i8")]
        Int8 = 10,

        [Display(Name = "Int4", ShortName = "i4")]
        Int4 = 11,

        [Display(Name = "Int16", ShortName = "i16")]
        Int16 = 12,

        [Display(Name = "Int32", ShortName = "i32")]
        Int32 = 13,

        [Display(Name = "Int64", ShortName = "i64")]
        Int64 = 14,

        // GGML block quantization
        [Display(Name = "Q8_0", ShortName = "q8_0")]
        Q8_0 = 100,

        [Display(Name = "Q8_1", ShortName = "q8_1")]
        Q8_1 = 101,

        [Display(Name = "Q8_K", ShortName = "q8_k")]
        Q8_K = 102,

        [Display(Name = "Q6_K", ShortName = "q6_k")]
        Q6_K = 103,

        [Display(Name = "Q5_0", ShortName = "q5_0")]
        Q5_0 = 104,

        [Display(Name = "Q5_1", ShortName = "q5_1")]
        Q5_1 = 105,

        [Display(Name = "Q5_K", ShortName = "q5_k")]
        Q5_K = 106,

        [Display(Name = "Q4_0", ShortName = "q4_0")]
        Q4_0 = 107,

        [Display(Name = "Q4_1", ShortName = "q4_1")]
        Q4_1 = 108,

        [Display(Name = "Q4_K", ShortName = "q4_k")]
        Q4_K = 109,

        [Display(Name = "Q3_K", ShortName = "q3_k")]
        Q3_K = 110,

        [Display(Name = "Q2_K", ShortName = "q2_k")]
        Q2_K = 111,

        [Display(Name = "Q2_0", ShortName = "q2_0")]
        Q2_0 = 112,

        [Display(Name = "Q1_0", ShortName = "q1_0")]
        Q1_0 = 113,

        // GGML importance quantization
        [Display(Name = "IQ4_NL", ShortName = "iq4_nl")]
        IQ4_NL = 120,

        [Display(Name = "IQ4_XS", ShortName = "iq4_xs")]
        IQ4_XS = 121,

        [Display(Name = "IQ3_S", ShortName = "iq3_s")]
        IQ3_S = 122,

        [Display(Name = "IQ3_XXS", ShortName = "iq3_xxs")]
        IQ3_XXS = 123,

        [Display(Name = "IQ2_S", ShortName = "iq2_s")]
        IQ2_S = 124,

        [Display(Name = "IQ2_XS", ShortName = "iq2_xs")]
        IQ2_XS = 125,

        [Display(Name = "IQ2_XXS", ShortName = "iq2_xxs")]
        IQ2_XXS = 126,

        [Display(Name = "IQ1_S", ShortName = "iq1_s")]
        IQ1_S = 127,

        [Display(Name = "IQ1_M", ShortName = "iq1_m")]
        IQ1_M = 128,

        // GGML ternary and microscaling
        [Display(Name = "TQ1_0", ShortName = "tq1_0")]
        TQ1_0 = 140,

        [Display(Name = "TQ2_0", ShortName = "tq2_0")]
        TQ2_0 = 141,

        [Display(Name = "MXFP4", ShortName = "mxfp4")]
        MXFP4 = 142,

        [Display(Name = "NVFP4", ShortName = "nvfp4")]
        NVFP4 = 143
    }
}
