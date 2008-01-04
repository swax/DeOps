using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Text;
using System.Windows.Forms;

using DeOps.Components.Plan.Schedule;

namespace DeOps.Components.Plan
{

    public partial class DateSlider : UserControl
    {
        ScheduleView Schedule;

        long YearTicks;
        long QuarterYearTicks;
        long MonthTicks;
        long WeekTicks;
        long DayTicks;
        long QuarterDayTicks;
        long HourTicks;

        enum TickType { Year, QuarterYear, Month, Week, Day, QuarterDay, Hour, None};

        internal int RefMark;
        internal List<int> BigMarks = new List<int>();
        internal List<int> SmallMarks = new List<int>();

        long StartTick;
        long EndTick;
        long TickSpan;
        internal long TicksperPixel;

        TickType BigTick = TickType.None;
        TickType SmallTick = TickType.None;

        bool Sliding;
        int  Slide_StartPixel;
        long Slide_StartTick;
        long Slide_EndTick;

        bool LeftArrowPressed;
        bool RightArrowPressed;


        public DateSlider()
        {
            InitializeComponent();

            YearTicks   = new TimeSpan(365, 0, 0, 0, 0).Ticks;
            QuarterYearTicks = new TimeSpan(365/4, 0, 0, 0, 0).Ticks;
            MonthTicks  = new TimeSpan(30, 0, 0, 0, 0).Ticks;
            WeekTicks   = new TimeSpan(7, 0, 0, 0, 0).Ticks;
            DayTicks    = new TimeSpan(1, 0, 0, 0, 0).Ticks;
            QuarterDayTicks = new TimeSpan(0, 6, 0, 0, 0).Ticks;
            HourTicks   = new TimeSpan(0, 1, 0, 0, 0).Ticks;

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }

        internal void Init(ScheduleView view)
        {
            Schedule = view;
        }

        private void DateSlider_Resize(object sender, EventArgs e)
        {
            if (Schedule == null)
                return;


            if (Width > 0 && Height > 0)
            {
                DisplayBuffer = new Bitmap(Width, Height);

                DateTime start = Schedule.StartTime;
                Schedule.EndTime = start.AddTicks(TicksperPixel * Width);

                RefreshSlider();
            }
        }

        internal void RefreshSlider()
        {
            if (Width <= 0)
                return;

            DateTime start = Schedule.StartTime;
            DateTime end = Schedule.EndTime;

            StartTick = start.Ticks;
            EndTick = end.Ticks;

            TickSpan = EndTick - StartTick;
            TicksperPixel = TickSpan / Width;

            GetTickType(ref BigTick, ref SmallTick);

            BigMarks = GetTickMarks(BigTick);
            SmallMarks = GetTickMarks(SmallTick);

            RefMark = (int)((EndTick-StartTick) / 4 / TicksperPixel);

            UpdateExtended(start);

            Redraw = true;

            Schedule.RefreshRows();

            Refresh();
        }

        private void UpdateExtended(DateTime start)
        {
            switch (BigTick)
            {
                case TickType.Year:
                    Schedule.ExtendedLabel.Text = "";
                    break;
                case TickType.QuarterYear:
                    Schedule.ExtendedLabel.Text = "";
                    break;
                case TickType.Month:
                    Schedule.ExtendedLabel.Text = "";
                    break;
                case TickType.Week:
                    Schedule.ExtendedLabel.Text = start.ToString("yyyy");
                    break;
                case TickType.Day:
                    Schedule.ExtendedLabel.Text = start.ToString("MMMM yyyy");
                    break;
                case TickType.QuarterDay:
                    Schedule.ExtendedLabel.Text = start.ToString("MMMM yyyy");
                    break;
                case TickType.Hour:
                    Schedule.ExtendedLabel.Text = start.ToString("dddd, MMMM %d, yyyy");
                    break;
            }
        }

        private List<int> GetTickMarks(TickType tickType)
        {
            List<int> marks = new List<int>();

            if (tickType == TickType.None)
                return marks;

            DateTime pos = GetStartPos(tickType);
            long posTick = pos.Ticks;

            while (posTick < EndTick)
            {
                if (posTick > StartTick)
                    marks.Add((int)((posTick - StartTick) / TicksperPixel));

                pos = IterStartPos(tickType, pos);
                posTick = pos.Ticks;
            }

            // add one extra so when text drawn it scrolls smoothly off the right
            marks.Add((int)((posTick - StartTick) / TicksperPixel)); 

            return marks;
        }

        private void GetTickType(ref TickType bigTick, ref TickType smallTick)
        {
            if (TickSpan > YearTicks)
            {
                bigTick = TickType.Year;
                smallTick = TickType.QuarterYear;
            }

            else if (TickSpan > QuarterYearTicks)
            {
                bigTick = TickType.QuarterYear;
                smallTick = TickType.Month;
            }

            else if (TickSpan > MonthTicks)
            {
                bigTick = TickType.Month;
                smallTick = TickType.Week;
            }

            else if (TickSpan > WeekTicks)
            {
                bigTick = TickType.Week;
                smallTick = TickType.Day;
            }

            else if (TickSpan > DayTicks)
            {
                bigTick = TickType.Day;
                smallTick = TickType.QuarterDay;
            }

            else if (TickSpan > QuarterDayTicks)
            {
                bigTick = TickType.QuarterDay;
                smallTick = TickType.Hour;
            }

            else if (TickSpan > HourTicks)
            {
                bigTick = TickType.Hour;
                smallTick = TickType.None;
            }

            else
            {
                bigTick = TickType.None;
                smallTick = TickType.None;
            }
        }

        DateTime GetStartPos(TickType tickType)
        {
            DateTime start = Schedule.StartTime;

            switch (tickType)
            {
                case TickType.Year:
                    return new DateTime(start.Year, 1, 1, 0, 0, 0);

                case TickType.QuarterYear:
                    return new DateTime(start.Year, 1, 1, 0, 0, 0);

                case TickType.Month:
                    return new DateTime(start.Year, start.Month, 1, 0, 0, 0);

                case TickType.Week:
                    start = new DateTime(start.Year, start.Month, 1, 0, 0, 0);

                    while (start.DayOfWeek != DayOfWeek.Sunday)
                        start = start.AddDays(1);

                    return start;

                case TickType.Day:
                    return new DateTime(start.Year, start.Month, start.Day, 0, 0, 0);

                case TickType.QuarterDay:
                    return new DateTime(start.Year, start.Month, start.Day, 0, 0, 0);

                case TickType.Hour:
                    return new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0);
            }

            return start;
        }

        DateTime IterStartPos(TickType tickType, DateTime pos)
        {
            switch (tickType)
            {
                case TickType.Year:
                    return pos.AddYears(1);

                case TickType.QuarterYear:
                    return pos.AddMonths(3);

                case TickType.Month:
                    return pos.AddMonths(1);

                case TickType.Week:
                    return pos.AddDays(7);

                case TickType.Day:
                    return pos.AddDays(1);

                case TickType.QuarterDay:
                    return pos.AddHours(6);

                case TickType.Hour:
                    return pos.AddHours(1);
            }

            return pos;
        }

        Bitmap DisplayBuffer;
        bool Redraw;

        Pen BlackPen = new Pen(Color.Black);
        Pen RefPen = new Pen(Color.LawnGreen, 2);
        Brush ControlBrush = new SolidBrush(SystemColors.ControlLight);

        private void DateSlider_Paint(object sender, PaintEventArgs e)
        {
            if (Schedule == null)
                return;

            if(DisplayBuffer == null )
                DisplayBuffer = new Bitmap(Width, Height);

            if (!Redraw)
            {
                e.Graphics.DrawImage(DisplayBuffer, 0, 0);
                return;
            }
            Redraw = false;

            // background
            Graphics buffer = Graphics.FromImage(DisplayBuffer);

            buffer.Clear(SystemColors.ControlLight);
            buffer.SmoothingMode = SmoothingMode.AntiAlias;

            // marks
            foreach (int mark in BigMarks)
                buffer.DrawLine(BlackPen, mark, Height * 4/8, mark, Height);

            foreach (int mark in SmallMarks)
                buffer.DrawLine(BlackPen, mark, Height *7/8, mark, Height);

            // ref line
            buffer.DrawLine(RefPen, RefMark, Height * 6/8, RefMark, Height);

            // text
            DrawText(buffer);
            
            // arrows
            buffer.FillRectangle(ControlBrush, GetLeftArrowZone());
            buffer.DrawImage(DateArrows.Left,  new Rectangle(0,6,16,16));

            buffer.FillRectangle(ControlBrush, GetRightArrowZone());
            buffer.DrawImage(DateArrows.Right, new Rectangle(Width - 16, 6, 16, 16));

            // Copy buffer to display
            e.Graphics.DrawImage(DisplayBuffer, 0, 0);
        }

        SolidBrush BlackBrush = new SolidBrush(Color.Black);


        Font BigFont = new Font("Tahoma", 7, FontStyle.Bold);
        Font SmallFont = new Font("Tahoma", 6);

        void DrawText(Graphics buffer)
        {
            Dictionary<int, string> labelMap = new Dictionary<int, string>();

            if (GetDateLabels(SmallMarks, SmallTick, labelMap, buffer))
                foreach (KeyValuePair<int, string> pair in labelMap)
                {
                    int x = pair.Key - (int)(buffer.MeasureString(pair.Value, SmallFont).Width / 2);

                    buffer.DrawString(pair.Value, SmallFont, BlackBrush, new PointF(x, 15));
                }


            if (GetDateLabels(BigMarks, BigTick, labelMap, buffer))
                foreach (KeyValuePair<int, string> pair in labelMap)
                {
                    int x = pair.Key - (int)(buffer.MeasureString(pair.Value, BigFont).Width / 2);

                    buffer.DrawString(pair.Value, BigFont, BlackBrush, new PointF(x, 2));
                }
        }


        bool GetDateLabels(List<int> marks, TickType tickType, Dictionary<int, string> labelMap, Graphics buffer)
        {
            labelMap.Clear();

            DateTime approxTime;
            string smallLabel = "";
            string bigLabel = "";

            int space = 0;
            if (marks.Count > 1)
                space = marks[1] - marks[0];

            foreach (int mark in marks)
            {
                int prevMark = mark - space;

                approxTime = new DateTime(StartTick + (prevMark + space / 2) * TicksperPixel);

                GetSpaceText(tickType, approxTime, ref bigLabel, ref smallLabel);

                if (buffer.MeasureString(bigLabel, BigFont).Width < space - 4)
                    labelMap[prevMark + space / 2] = bigLabel;

                else if (buffer.MeasureString(smallLabel, BigFont).Width < space - 4)
                    labelMap[prevMark + space / 2] = smallLabel;

                else
                    return false;
            }

            return true;
        }


        private void GetSpaceText(TickType tickType, DateTime time, ref string big, ref string small)
        {
            switch (tickType)
            {
                case TickType.Year:
                    big = time.Year.ToString();
                    small = time.ToString("yy");
                    break;

                case TickType.QuarterYear:
                    big = "Q" + ((time.Month / 4) + 1).ToString() + time.ToString("/yyyy");
                    small = "Q" + ((time.Month / 4) + 1).ToString();
                    break;

                case TickType.Month:
                    big = time.ToString("MMM/yyyy");
                    small = time.ToString("MMM");
                    break;

                case TickType.Week:
                    big = "W" + ((time.Day / 7) + 1).ToString() + time.ToString("/MMM");
                    small = "W" + ((time.Day / 7) + 1).ToString();
                    break;

                case TickType.Day:
                    big = time.ToString("ddd (%d)");
                    small = time.ToString("%d");
                    break;

                case TickType.QuarterDay:
                    DateTime QuarterTime = time;
                    QuarterTime = QuarterTime.AddHours(-(time.Hour % 6));

                    big = QuarterTime.ToString("ddd %ht") + "-" + QuarterTime.AddHours(6).ToString("%ht");
                    small = QuarterTime.ToString("%h") + "-" + QuarterTime.AddHours(6).ToString("%h");
                    break;

                case TickType.Hour:
                    big = time.ToString("ddd %h tt");
                    small = time.ToString("%ht");
                    break;
            }
        }

        Rectangle GetLeftArrowZone()
        {
            return new Rectangle(-1, 0, 18, 27);
        }
        
        Rectangle GetRightArrowZone()
        {
            return new Rectangle(Width - 18, 0, 18, 27);
        }

        private void DateSlider_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            if( GetLeftArrowZone().Contains(e.Location))
            {
                LeftArrowPressed = true;
                ButtonTimer.Enabled = true;
                return;
            }

            if (GetRightArrowZone().Contains(e.Location))
            {
                RightArrowPressed = true;
                ButtonTimer.Enabled = true;
                return;
            }
            
            
            Sliding = true;
            Slide_StartPixel = e.X;

            Slide_StartTick = StartTick;
            Slide_EndTick = EndTick;
        }

        private void DateSlider_MouseMove(object sender, MouseEventArgs e)
        {
            if (!Sliding)
                return;

            int delta = e.X - Slide_StartPixel;

            MoveSlider(delta);
        }

        private void MoveSlider(int delta)
        {
            long deltaTicks = delta * TicksperPixel;

            TimeSpan span = new TimeSpan(deltaTicks);

            Schedule.StartTime = new DateTime(Slide_StartTick - deltaTicks);
            Schedule.EndTime = new DateTime(Slide_EndTick - deltaTicks);

            RefreshSlider();
        }

        private void DateSlider_MouseUp(object sender, MouseEventArgs e)
        {
            Sliding = false;
            LeftArrowPressed = false;
            RightArrowPressed = false;

            ButtonTimer.Enabled = false;
        }

        private void ButtonTimer_Tick(object sender, EventArgs e)
        {
            Slide_StartTick = StartTick;
            Slide_EndTick = EndTick;

            if (LeftArrowPressed)
                MoveSlider(4);

            if (RightArrowPressed)
                MoveSlider(-4);
        }
    }
}
