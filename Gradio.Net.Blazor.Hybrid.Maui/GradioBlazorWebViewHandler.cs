using Microsoft.AspNetCore.Components.WebView.Maui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradio.Net
{
    internal partial class GradioBlazorWebViewHandler : BlazorWebViewHandler
    {
        private SynchronizationContext _currentSynchronizationContext;

        public GradioBlazorWebViewHandler()
        {
            _currentSynchronizationContext = SynchronizationContext.Current!;

        }
    }
}
