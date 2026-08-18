using System;

namespace Amuse.Host.StableDiffusionCpp
{
    public class StableDiffusionApiException : Exception
    {
        public StableDiffusionApiException(string message) : base(message) { }
        public StableDiffusionApiException(string message, Exception inner) : base(message, inner) { }
    }

}

