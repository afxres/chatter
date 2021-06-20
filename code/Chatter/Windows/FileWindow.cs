using Chatter.Internal;
using Mikodev.Links.Abstractions;
using System;
using System.Collections.Generic;

namespace Chatter.Windows
{
    public sealed class FileWindow : SharingWindow
    {
        private readonly SharingViewer viewer;

        private readonly List<(double progress, double speed)> values = new List<(double, double)>();

        public FileWindow(ISharingFileObject fileObject) : base(fileObject)
        {
            this.viewer = fileObject.Viewer;
        }

        protected override void OnUpdate(string propertyName)
        {
            base.OnUpdate(propertyName);
            this.UpdateNotice($@"{Extensions.ToUnit(this.viewer.Position)} / {Extensions.ToUnit(this.viewer.Length)}, {100.0 * this.viewer.Progress:0.00}%, {this.viewer.Remaining:hh\:mm\:ss}, {this.viewer.Status}");
            if (propertyName != nameof(SharingViewer.Progress) && (this.viewer.Status & SharingStatus.Completed) == 0)
                return;
            var count = this.values.Count;
            if (count > 1 && Math.Abs(this.values[count - 1].progress - this.values[count - 2].progress) < 0.002)
                this.values.RemoveAt(count - 1);
            this.values.Add((this.viewer.Progress, this.viewer.Speed));
            this.UpdateGraphics(this.values);
        }
    }
}
