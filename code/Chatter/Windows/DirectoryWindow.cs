using Chatter.Internal;
using Mikodev.Links.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Chatter.Windows
{
    public sealed class DirectoryWindow : SharingWindow
    {
        private readonly SharingViewer viewer;

        private readonly Stopwatch stopwatch = Stopwatch.StartNew();

        private readonly List<(double progress, double speed)> values = new List<(double, double)>();

        private readonly List<(TimeSpan timeSpan, double speed)> history = new List<(TimeSpan, double)>();

        public DirectoryWindow(ISharingDirectoryObject directoryObject) : base(directoryObject)
        {
            this.viewer = directoryObject.Viewer;
        }

        protected override void OnUpdate(string propertyName)
        {
            base.OnUpdate(propertyName);

            this.UpdateNotice($"{Extensions.ToUnit(this.viewer.Position)}, {this.viewer.Status}");
            if (propertyName != nameof(SharingViewer.Speed) && (this.viewer.Status & SharingStatus.Completed) == 0)
                return;

            var count = this.history.Count;
            if (count > 1 && this.history[count - 1].timeSpan - this.history[count - 2].timeSpan < TimeSpan.FromMilliseconds(33))
                this.history.RemoveAt(count - 1);
            var limits = TimeSpan.FromSeconds(30);
            var timeSpan = this.stopwatch.Elapsed;
            var standard = timeSpan - limits;
            this.history.Add((timeSpan, this.viewer.Speed));
            var first = this.history.FirstOrDefault(x => x.timeSpan > standard);
            var index = this.history.IndexOf(first);
            if (index > 0)
                this.history.RemoveRange(0, index);
            this.values.Clear();
            var offset = first.timeSpan;
            var total = timeSpan - first.timeSpan;
            if (total > limits)
                total = limits;
            foreach (var (span, speed) in this.history)
                this.values.Add((1.0 * (span - first.timeSpan).TotalMilliseconds / total.TotalMilliseconds, speed));
            this.UpdateGraphics(this.values);
        }
    }
}
