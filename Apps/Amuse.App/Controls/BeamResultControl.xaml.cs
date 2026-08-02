using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TensorStack.Common;

namespace Amuse.App.Controls
{
    /// <summary>
    /// Interaction logic for BeamResultControl.xaml
    /// </summary>
    public partial class BeamResultControl : TokenStreamBaseControl
    {
        private List<TextInput> _beamResults;
        private TextInput _selectedBeamResult;

        ///// <summary>
        /// Initializes a new instance of the <see cref="BeamResultControl"/> class.
        /// </summary>
        public BeamResultControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty IsMultipleBeamResultProperty = DependencyProperty.Register(nameof(IsMultipleBeamResult), typeof(bool), typeof(BeamResultControl), new PropertyMetadata(false));

        public bool IsMultipleBeamResult
        {
            get { return (bool)GetValue(IsMultipleBeamResultProperty); }
            set { SetValue(IsMultipleBeamResultProperty, value); }
        }

        public List<TextInput> BeamResults
        {
            get { return _beamResults; }
            set { SetProperty(ref _beamResults, value); }
        }

        public TextInput SelectedBeamResult
        {
            get { return _selectedBeamResult; }
            set { SetProperty(ref _selectedBeamResult, value); }
        }


        /// <summary>
        /// Adds the beam results.
        /// </summary>
        /// <param name="results">The results.</param>
        public void AddBeamResults(List<TextInput> results)
        {
            if (!IsMultipleBeamResult)
                return;

            BeamResults = results;
            SelectedBeamResult = BeamResults?.FirstOrDefault();
        }


        /// <summary>
        /// Clears thes control
        /// </summary>
        public override async Task ClearAsync()
        {
            await base.ClearAsync();
            await ClearConversationAsync();
            BeamResults = null;
            SelectedBeamResult = null;
        }


        /// <summary>
        /// Clear conversation
        /// </summary>
        public override async Task ClearConversationAsync()
        {
            await base.ClearConversationAsync();
            await ResultControl.ClearAsync();
        }


        /// <summary>
        /// Add assistant response
        /// </summary>
        /// <param name="response">The response.</param>
        public override async Task AddAssistantResponseAsync(string response)
        {
            await base.AddAssistantResponseAsync(response);
            await ResultControl.AppendTextAsync(CurrentResult.Content);
        }


        /// <summary>
        /// End the stream response 
        /// </summary>
        public override async Task<string> EndStreamResponseAsync()
        {
            var response = await base.EndStreamResponseAsync();
            if (response == null)
                return response;

            await ResultControl.AppendTextAsync("\n");
            return response;
        }


        /// <summary>
        /// Updates the stream response.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="tokens">The tokens.</param>
        public override void UpdateStreamResponse(string token, int tokens)
        {
            if (IsMultipleBeamResult)
                return;
            base.UpdateStreamResponse(token, tokens);
        }


        /// <summary>
        /// On stream flush
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        protected override async Task OnStreamFlushAsync(string buffer)
        {
            await base.OnStreamFlushAsync(buffer);
            await ResultControl.AppendStreamAsync(buffer);
        }


        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseEnter" /> attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            if (!IsKeyboardFocusWithin)
                Focus();
        }
    }
}
