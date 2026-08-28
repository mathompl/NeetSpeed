using System;
using System.Collections;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NetSpeed
{
    class Drawing
    {
        SolidBrush drawBrushBG;
        SolidBrush drawBrushBlack;
        SolidBrush drawBrushBlue;
        SolidBrush drawBrushUP;
        SolidBrush drawBrushDL;
        SolidBrush drawBrushYellow;
        SolidBrush drawBrushAvg;

        Font font;
        Font fontsm;

        Form form;
        NotifyIcon icon;
        Config config;
        IList dataIn;
        IList dataOut;

        public Drawing(Form form, Config config, NotifyIcon notifyIcon1)
        {
            this.form = form;
            this.icon = notifyIcon1;
            this.config = config;
            initBrushes();
            initFonts();
        }

        public void paintAll(double bytesReceivedSpeed, int avgIn, double bytesSentSpeed, int avgOut,
            IList dataIn, IList dataOut)
        {
            try
            {
                if (form == null || config == null)
                    return;

                EnsureAlive();
                initFonts();

                this.dataIn = dataIn ?? new ArrayList();
                this.dataOut = dataOut ?? new ArrayList();

                paintTray(bytesReceivedSpeed, avgIn, bytesSentSpeed, avgOut);
                paintCharts(bytesReceivedSpeed, avgIn, bytesSentSpeed, avgOut);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("paintAll: " + ex);
            }
        }

        private void EnsureAlive()
        {
            if (drawBrushBG == null)
                initBrushes();
        }

        public void initBrushes()
        {
            disposeBrushes();
            drawBrushBG = new SolidBrush(Color.Gray);
            drawBrushBlack = new SolidBrush(Color.Black);
            drawBrushBlue = new SolidBrush(Color.DarkBlue);
            drawBrushAvg = new SolidBrush(Color.DarkBlue);
            drawBrushUP = new SolidBrush(Color.Red);
            drawBrushDL = new SolidBrush(Color.Green);
            drawBrushYellow = new SolidBrush(Color.Yellow);
        }

        private void initFonts()
        {
            float size = config != null ? (float)config.font : 8f;
            float sizeSm = config != null ? (float)config.fontLegend : 7f;
            if (size < 6f) size = 6f;
            if (size > 48f) size = 48f;
            if (sizeSm < 5f) sizeSm = 5f;
            if (sizeSm > 24f) sizeSm = 24f;

            if (font != null) font.Dispose();
            if (fontsm != null) fontsm.Dispose();

            try
            {
                font = new Font("Consolas", size);
                fontsm = new Font("Consolas", sizeSm);
            }
            catch
            {
                font = new Font(FontFamily.GenericMonospace, size);
                fontsm = new Font(FontFamily.GenericMonospace, sizeSm);
            }
        }

        public void disposeBrushes()
        {
            if (drawBrushBG != null) { drawBrushBG.Dispose(); drawBrushBG = null; }
            if (drawBrushBlack != null) { drawBrushBlack.Dispose(); drawBrushBlack = null; }
            if (drawBrushBlue != null) { drawBrushBlue.Dispose(); drawBrushBlue = null; }
            if (drawBrushUP != null) { drawBrushUP.Dispose(); drawBrushUP = null; }
            if (drawBrushDL != null) { drawBrushDL.Dispose(); drawBrushDL = null; }
            if (drawBrushYellow != null) { drawBrushYellow.Dispose(); drawBrushYellow = null; }
            if (drawBrushAvg != null) { drawBrushAvg.Dispose(); drawBrushAvg = null; }
        }

        private static int ToIntSample(object item)
        {
            if (item == null)
                return 0;
            try
            {
                long v = Convert.ToInt64(item);
                if (v < 0) return 0;
                if (v > int.MaxValue) return int.MaxValue;
                return (int)v;
            }
            catch
            {
                return 0;
            }
        }

        private static double Safe(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v) || v < 0)
                return 0;
            return v;
        }

        private static string FormatSpeed(double kbPerSec)
        {
            kbPerSec = Safe(kbPerSec);
            if (kbPerSec >= 1024.0 * 1024.0)
                return Math.Round(kbPerSec / (1024.0 * 1024.0), 2) + " GB/s";
            if (kbPerSec >= 1024.0)
                return Math.Round(kbPerSec / 1024.0, 2) + " MB/s";
            return Math.Round(kbPerSec, 2) + " KB/s";
        }

        private static string FormatRate(int kbPerSec)
        {
            return FormatSpeed(kbPerSec);
        }

        public static Icon BitmapToIcon(Bitmap bitmap)
        {
            IntPtr handle = bitmap.GetHicon();
            try
            {
                using (Icon tmp = Icon.FromHandle(handle))
                    return (Icon)tmp.Clone();
            }
            finally
            {
                DestroyIcon(handle);
            }
        }

        public void paintChart(string stream, Graphics c, SolidBrush color, int x, int y,
            int samples, int height, IList data, string speed)
        {
            if (c == null || data == null || samples <= 0 || height <= 0 || font == null)
                return;

            while (data.Count > samples)
                data.RemoveAt(0);

            if (data.Count == 0)
            {
                c.DrawString("0 " + stream, font, drawBrushYellow, 12, y - 12);
                return;
            }

            int max = 0;
            long sum = 0;
            for (int i = 0; i < data.Count; i++)
            {
                int v = ToIntSample(data[i]);
                if (v > max) max = v;
                sum += v;
            }

            int avg = (int)(sum / data.Count);
            int paintheight = height < 1 ? 1 : height;
            if (max <= 0)
                max = paintheight;

            double scale = (double)paintheight / (double)max;
            int avgy = y - (int)(avg * scale) + paintheight;

            if (config != null && config.paintAvg)
            {
                using (Pen p = new Pen(drawBrushAvg))
                    c.DrawLine(p, x, avgy, samples + x, avgy);
            }
            int offset = 0;
            if (offset < 0)
                offset = 0;

            using (Pen linePen = new Pen(color))
            {
                for (int i = 1; i < data.Count; i++)
                {
                    int lastY = (int)(ToIntSample(data[i - 1]) * scale);
                    int nowY = (int)(ToIntSample(data[i]) * scale);
                    c.DrawLine(
                        linePen,
                        x + offset + i - 1, y - lastY + paintheight,
                        x + offset + i, y - nowY + paintheight);
                }
            }

            const int pad = 4;

            SizeF szMax = c.MeasureString(FormatRate(max), fontsm);
            SizeF szMid = c.MeasureString(FormatRate(max / 2), fontsm);

            float labelX = x + samples - pad - Math.Max(szMax.Width, szMid.Width);
            if (labelX < x + pad)
                labelX = x + pad;

            float yMax = y + pad;
            float yMid = y + paintheight / 2f - szMid.Height / 2f;

            if (yMax + szMax.Height > y + paintheight)
                yMax = y + paintheight - szMax.Height - pad;
            if (yMid < y + pad)
                yMid = y + pad;
            if (yMid + szMid.Height > y + paintheight)
                yMid = y + paintheight - szMid.Height - pad;

            c.DrawString(FormatRate(max), fontsm, drawBrushYellow, labelX, yMax);
            c.DrawString(FormatRate(max / 2), fontsm, drawBrushYellow, labelX, yMid);
            c.DrawString(speed + " " + stream + " (avg " + FormatRate(avg) + ")", font, drawBrushYellow, 12, y - 12);
        }

        public void paintCharts(double bytesReceivedSpeed, int avgIn, double bytesSentSpeed, int avgOut)
        {
            if (form == null || form.Width < 20 || form.Height < 20)
                return;

            string sent = FormatSpeed(bytesSentSpeed);
            string received = FormatSpeed(bytesReceivedSpeed);

            int w = Math.Max(1, form.Width);
            int h = Math.Max(1, form.Height);

            Bitmap canvas = null;
            try
            {
                canvas = TryLoadMetal(w, h);

                using (Graphics imageCanvas = Graphics.FromImage(canvas))
                {
                    imageCanvas.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                    imageCanvas.FillRectangle(drawBrushBlack, 6, 6, Math.Max(1, w - 12), Math.Max(1, h - 12));

                    int maxX = Math.Min(w - 6, canvas.Width);
                    int maxY = Math.Min(h - 6, canvas.Height);
                    for (int i = 6; i < maxX; i += 4)
                    {
                        for (int k = 6; k < maxY; k += 4)
                            canvas.SetPixel(i, k, Color.DarkBlue);
                    }

                    using (Pen gridPen = new Pen(drawBrushBG))
                        imageCanvas.DrawLine(gridPen, 6, h / 2, w - 6, h / 2);

                    paintChart("in", imageCanvas, drawBrushDL, 10, 18, w - 20, h / 2 - 20, dataIn, received);
                    paintChart("out", imageCanvas, drawBrushUP, 10, h / 2 + 12, w - 20, h / 2 - 20, dataOut, sent);
                }

                Image old = form.BackgroundImage;
                form.BackgroundImage = canvas;
                canvas = null;
                if (old != null)
                    old.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("paintCharts: " + ex);
                if (canvas != null)
                    canvas.Dispose();
            }
        }

        private static Bitmap TryLoadMetal(int width, int height)
        {
            try
            {
                Bitmap src = global::NetSpeed.Properties.Resources.metal;
                return new Bitmap(src, Math.Max(1, width), Math.Max(1, height));
            }
            catch
            {
                return new Bitmap(Math.Max(1, width), Math.Max(1, height));
            }
        }

        public void paintTray(double bytesReceivedSpeed, int avgIn, double bytesSentSpeed, int avgOut)
        {
            if (icon == null || config == null)
                return;

            Bitmap bmp = null;
            try
            {
                bmp = new Bitmap(16, 16);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.FillRectangle(drawBrushBG, 0, 0, 16, 16);
                    using (Pen border = new Pen(drawBrushBlack))
                        g.DrawRectangle(border, 0f, 0f, 15f, 15f);

                    int maxDl = config.max <= 0 ? 1 : config.max;
                    int maxUp = config.maxup <= 0 ? 1 : config.maxup;

                    int scaleDl = (int)(Safe(bytesReceivedSpeed) * 15.0 / maxDl);
                    if (scaleDl > 15) scaleDl = 15;
                    if (scaleDl < 0) scaleDl = 0;

                    int scaleUp = (int)(Safe(bytesSentSpeed) * 15.0 / maxUp);
                    if (scaleUp > 15) scaleUp = 15;
                    if (scaleUp < 0) scaleUp = 0;

                    if (scaleDl > 0)
                        g.FillRectangle(drawBrushDL, 2, 16 - scaleDl, 5, scaleDl);
                    if (scaleUp > 0)
                        g.FillRectangle(drawBrushUP, 9, 16 - scaleUp, 5, scaleUp);

                    if (config.paintAvg)
                    {
                        int avgInScale = avgIn * 15 / maxDl;
                        if (avgInScale > 15) avgInScale = 15;
                        if (avgInScale < 0) avgInScale = 0;
                        using (Pen p = new Pen(drawBrushAvg))
                            g.DrawLine(p, 2, 16 - avgInScale, 6, 16 - avgInScale);

                        int avgOutScale = avgOut * 15 / maxUp;
                        if (avgOutScale > 15) avgOutScale = 15;
                        if (avgOutScale < 0) avgOutScale = 0;
                        using (Pen p = new Pen(drawBrushAvg))
                            g.DrawLine(p, 9, 16 - avgOutScale, 13, 16 - avgOutScale);
                    }
                }

                double inPercent = Safe(bytesReceivedSpeed) / Math.Max(1, config.max) * 100.0;
                double outPercent = Safe(bytesSentSpeed) / Math.Max(1, config.maxup) * 100.0;

                string text = "DL: " + FormatSpeed(bytesReceivedSpeed) + " (" + Math.Round(inPercent, 2) + "%)\n" +
                              "UP: " + FormatSpeed(bytesSentSpeed) + " (" + Math.Round(outPercent, 2) + "%)";
                if (text.Length > 63)
                    text = text.Substring(0, 63);

                Icon oldIcon = icon.Icon;
                icon.Icon = BitmapToIcon(bmp);
                icon.Text = text;
                if (oldIcon != null)
                    oldIcon.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("paintTray: " + ex);
            }
            finally
            {
                if (bmp != null)
                    bmp.Dispose();
            }
        }

        [DllImport("user32.dll", EntryPoint = "DestroyIcon")]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}