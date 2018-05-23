namespace Zebble
{
    using System;
    using System.Threading.Tasks;

    public class Slider : View, FormField.IControl
    {
        double? lowValue, upValue;
        double minValue = 0, maxValue = 100;
        float? PixelsPerStep;
        View ActiveHandle;
        bool IsProcessingPan, Animating;
        TouchEventArgs LastPanEnd;
        Point CurrentVelocity;

        public readonly AsyncEvent ValueChanged = new AsyncEvent();
        public readonly AsyncEvent LowValueChanged = new AsyncEvent();
        public readonly AsyncEvent UpValueChanged = new AsyncEvent();

        public readonly Canvas RangeBar = new Canvas { Id = "RangeBar" };
        public readonly Canvas SelectedBar = new Canvas { Id = "SelectedBar" };
        public readonly Canvas Handle = new Canvas { Id = "Handle" };
        public readonly Canvas UpHandle = new Canvas { Id = "UpHandle" };
        public readonly TextView Caption = new TextView { Id = "Caption" }.Absolute().Width(30);
        public readonly TextView UpCaption = new TextView { Id = "UpCaption" }.Absolute().Width(30);

        public Slider()
        {
            Id = "Slider";

            Handles.Do(x => x.Css.Size(0)); // Without this the sizing goes wrong.

            ActiveHandle = Handle;

            Tapped.Handle(UserTapped);
            PanFinished.Handle(() => UserTapped(LastPanEnd));
            Panning.Handle(Panned);
        }

        public Slider(bool isRange) : this() => IsRange = isRange;

        public Canvas[] Handles => new[] { Handle, UpHandle };

        public TextView[] Captions => new[] { Caption, UpCaption };

        public bool IsRange { get; set; }

        public Func<double, string> CaptionText { get; set; } = f => f.ToString();

        public virtual double Value
        {
            get => LowValue;
            set
            {
                LowValue = value;
                if (IsRange) UpValue = MaxValue;
            }
        }

        TextView ActiveCaption => ActiveHandle == UpHandle ? UpCaption : Caption;

        object FormField.IControl.Value
        {
            get => Value;
            set
            {
                LowValue = Convert.ToDouble(value.ToStringOrEmpty().Or("0"));
                if (IsRange) UpValue = MaxValue;
            }
        }

        public virtual double LowValue
        {
            get => lowValue ?? MinValue;
            set
            {
                lowValue = Math.Max(MinValue, value);

                if (IsRange) lowValue = Math.Min(lowValue.Value, UpValue);

                if (IsRendered()) LoadValue(Handle, lowValue.Value);

                if (!IsRange) ValueChanged.Raise();
                else LowValueChanged.Raise();
            }
        }

        public virtual double UpValue
        {
            get => upValue ?? MaxValue;
            set
            {
                upValue = Math.Min(MaxValue, value);

                upValue = Math.Max(upValue.Value, LowValue);

                if (IsRendered()) LoadValue(UpHandle, upValue.Value);

                UpValueChanged.Raise();
            }
        }

        public double MinValue
        {
            get => minValue;
            set { minValue = value; if (lowValue == null || lowValue < value) LowValue = value; }
        }

        public double MaxValue
        {
            get => maxValue;
            set { maxValue = value; if (upValue == null || upValue > value) UpValue = value; }
        }

        public double Step { get; set; } = 1;

        public override async Task OnInitializing()
        {
            await base.OnInitializing();

            await AddRange(new View[] { RangeBar, SelectedBar, Handle, Caption });

            if (IsRange)
            {
                await Add(UpHandle);
                await Add(UpCaption);
            }

            await WhenShown(RearrangeElements);
        }

        void RearrangeElements()
        {
            var baseLine = ActualHeight - Math.Max(Handle.ActualHeight, UpHandle.ActualHeight) / 2 - RangeBar.ActualHeight / 2;

            RangeBar.Y.BindTo(Height, Handle.Height, UpHandle.Height, RangeBar.Height, Padding.Top, (total, h1, h2, b, pt) =>
            total - Math.Max(h1, h2) / 2 - b / 2 - pt);

            SelectedBar.Y.BindTo(RangeBar.Y);
            SelectedBar.Height.BindTo(RangeBar.Height);

            Handle.Y.BindTo(RangeBar.Y, RangeBar.Height, Handle.Height, (by, bh, hh) => by - (hh - bh) / 2);
            UpHandle.Y.BindTo(RangeBar.Y, RangeBar.Height, UpHandle.Height, (by, bh, hh) => by - (hh - bh) / 2);

            LoadValue(Handle, LowValue);
            if (IsRange) LoadValue(UpHandle, UpValue);
        }

        Task UserTapped(TouchEventArgs args)
        {
            if (Animating || args == null) return Task.CompletedTask;

            Animating = true;
            try
            {
                ActiveHandle = Handle;

                if (IsRange)
                {
                    var distanceFromHandle = Math.Abs(Handle.ActualX + Handle.ActualWidth / 2 - args.Point.X);
                    var distanceFromHandle2 = Math.Abs(UpHandle.ActualX + UpHandle.ActualWidth / 2 - args.Point.X);
                    if (IsRange && distanceFromHandle > distanceFromHandle2) ActiveHandle = UpHandle;
                }

                LoadValue(ActiveHandle, PointToValue(args.Point.X));
            }
            finally
            {
                Animating = false;
            }

            return Task.CompletedTask;
        }

        async Task Panned(PannedEventArgs args)
        {
            Animating = false;
            var start = args.From;
            var end = args.To;
            CurrentVelocity = args.Velocity;

            if (CheckDirection() == null) return;

            LastPanEnd = new TouchEventArgs(this, end, args.Touches);

            if (IsProcessingPan) return;
            IsProcessingPan = true;

            ActiveHandle = Handle;

            if (IsRange)
            {
                end.X = end.X.LimitWithin(Padding.Left() + ActiveHandle.ActualWidth / 2, RangeBar.ActualWidth + Padding.Left());

                var distanceFromHandle = Math.Abs(Handle.ActualX + Handle.ActualWidth / 2 - start.X);
                var distanceFromUpHandle = Math.Abs(UpHandle.ActualX + UpHandle.ActualWidth / 2 - start.X);

                if ((distanceFromHandle.AlmostEquals(distanceFromUpHandle, 1) && start.X < end.X) ||
                    distanceFromHandle > distanceFromUpHandle) ActiveHandle = UpHandle;
            }

            var point = (ActiveHandle.X.CurrentValue - (start.X - end.X)).LimitWithin(Padding.Left() + ActiveHandle.ActualWidth / 2, RangeBar.ActualWidth + Padding.Left());
            MoveElements(ActiveHandle, ActiveCaption, point, end.X);
            IsProcessingPan = false;
        }

        Direction? CheckDirection()
        {
            Direction? result;
            if (CurrentVelocity.X > 0) result = Direction.Right;
            else if (CurrentVelocity.X < 0) result = Direction.Left;
            else result = null;

            return result;
        }

        bool ShouldHandlerMove(float point)
        {
            return point <= -(Handle.ActualWidth / 2) || point >= ActualWidth - Handle.ActualWidth / 2;
        }

        void MoveElements(View handle, TextView caption, float point, float actualPoint)
        {
            var differences = handle.X.CurrentValue - point;
            if (differences == 0) return;

            var isArrangedProcess = (handle.X.CurrentValue == 0 && differences == actualPoint);

            ActiveCaption.Width(ActiveCaption.Text.Length * ActiveCaption.Font.EffectiveSize);

            if (isArrangedProcess || Animating) point = actualPoint;

            if (IsRange)
            {
                if (handle == Handle && point > UpHandle.ActualX + Handle.ActualWidth / 2)
                {
                    MoveElements(UpHandle, UpCaption, point, actualPoint);
                    return;
                }

                if (handle == UpHandle && point < Handle.ActualX + Handle.ActualWidth / 2)
                {
                    MoveElements(Handle, Caption, point, actualPoint);
                    return;
                }
            }

            if (!isArrangedProcess && ShouldHandlerMove(point)) return;

            point = Math.Min(point, RangeBar.ActualWidth + Padding.Left() - Handle.ActualWidth / 2);
            point = Math.Max(point, Handle.ActualWidth / 2 - Padding.Left());

            if (Animating)
            {
                var handleX = point - handle.ActualWidth / 2;

                var captionX = point - caption.ActualWidth / 2;
                captionX = Math.Min(captionX, ActualWidth - caption.ActualWidth);
                captionX = Math.Max(captionX, 0);
                captionX = Math.Min(handleX, captionX);

                handle.Animate(x => x.X(handleX));
                caption.Animate(x => x.X(captionX));
            }
            else
            {
                var direction = CheckDirection();
                if ((direction == Direction.Left && (point > handle.X.CurrentValue && point > Handle.ActualWidth / 2))
                    || (direction == Direction.Right && (point < handle.X.CurrentValue && point < ActualWidth - Handle.ActualWidth / 2))) return;
                
                handle.X(point);
                caption.X(point);
            }

            ActiveCaption.Text(CaptionText(PointToValue(point)));

            if (handle == Handle) SyncSelectedBar(point, UpHandle.ActualX);
            else SyncSelectedBar(Handle.ActualX, point);
        }

        double GetPixelsPerStep()
        {
            if (PixelsPerStep == null)
            {
                PixelsPerStep = (RangeBar.ActualWidth - Handle.ActualWidth) / (float)(MaxValue - MinValue);
            }

            return PixelsPerStep.Value;
        }

        void LoadValue(View handle, double value)
        {
            ActiveHandle = handle;
            ActiveCaption.Text(CaptionText(value));
            if (ActiveHandle == UpHandle)
            {
                upValue = value;
                UpValueChanged.Raise();
            }
            else
            {
                lowValue = value;
                if (!IsRange) ValueChanged.Raise();
                else LowValueChanged.Raise();
            }

            MoveElements(ActiveHandle, ActiveCaption, ValueToPoint(value), ValueToPoint(value));
        }

        float ValueToPoint(double value)
        {
            var diff = value - MinValue;
            var result = (float)(diff * GetPixelsPerStep()).Round(0);
            result += RangeBar.ActualX;
            result += ActiveHandle.ActualWidth / 2;
            return result;
        }

        double PointToValue(double point)
        {
            point -= ActiveHandle.ActualWidth / 2;
            var amount = point / GetPixelsPerStep() + MinValue;

            // Closest correct value:
            var smallestDiff = double.MaxValue;
            var result = MinValue;

            for (var value = MinValue; value <= MaxValue; value += Step)
            {
                var diff = Math.Abs(value - amount);

                if (diff > smallestDiff) break;  // Past the closest:

                smallestDiff = diff;
                result = value;
            }

            return result;
        }

        void SyncSelectedBar(float handleX, float upHandleX)
        {
            var midHanle = (handleX + Handle.ActualWidth / 2 - Padding.Left()).LimitMin(0);

            var newX = Padding.Left();

            var newWidth = midHanle;

            if (IsRange)
            {
                newX = midHanle + Padding.Left();

                var midUpHandle = (upHandleX + UpHandle.ActualWidth / 2 - Padding.Left()).LimitMin(0);
                newWidth = (upHandleX - handleX).LimitMin(0);
            }

            if (Animating)
            {
                SelectedBar.Animate(x => x.X(newX));
                SelectedBar.Animate(x => x.Width(newWidth));
            }
            else SelectedBar.X(newX).Width(newWidth);
        }

        public override void Dispose()
        {
            ValueChanged?.Dispose();
            LowValueChanged?.Dispose();
            UpValueChanged?.Dispose();
            base.Dispose();
        }
    }
}