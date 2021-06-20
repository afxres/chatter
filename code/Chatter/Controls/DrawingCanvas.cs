using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace Chatter.Controls
{
    public sealed class DrawingCanvas : Canvas
    {
        private readonly List<Visual> visuals = new List<Visual>();

        protected override int VisualChildrenCount => this.visuals.Count;

        protected override Visual GetVisualChild(int index) => this.visuals[index];

        public void AddVisual(Visual visual)
        {
            if (visual == null)
                throw new ArgumentNullException(nameof(visual));
            this.Dispatcher.VerifyAccess();

            if (this.visuals.Contains(visual))
                throw new ArgumentException();
            this.AddVisualChild(visual);
            this.AddLogicalChild(visual);
            this.visuals.Add(visual);
        }

        public bool RemoveVisual(Visual visual)
        {
            if (visual == null)
                throw new ArgumentNullException(nameof(visual));
            this.Dispatcher.VerifyAccess();

            if (!this.visuals.Remove(visual))
                return false;
            this.RemoveVisualChild(visual);
            this.RemoveLogicalChild(visual);
            return true;
        }

        public void ClearVisuals()
        {
            this.Dispatcher.VerifyAccess();

            this.visuals.ForEach(this.RemoveVisualChild);
            this.visuals.ForEach(this.RemoveLogicalChild);
            this.visuals.Clear();
        }
    }
}
