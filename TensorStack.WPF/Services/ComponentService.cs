// Copyright (c) Adam Clark. All rights reserved.
// Licensed under the Apache 2.0 License.
using Microsoft.Extensions.DependencyInjection;
using System;
using TensorStack.WPF.Controls;

namespace TensorStack.WPF.Services
{
    public class ComponentService
    {
        private IServiceProvider _serviceProvider;

        public ComponentService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T GetComponent<T>() where T : Component
        {
            return _serviceProvider.GetService<T>();
        }
    }
}
