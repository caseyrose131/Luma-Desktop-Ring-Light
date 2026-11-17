using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

static class Program {
    [STAThread] static void Main(string[] args) {
        Native.SetProcessDPIAware();
        if (args.Length > 0 && args[0] == "--test") {
            using (Bitmap b = Glow.Render(800, 600, 90, 0.8, Color.White)) {
                if (b.GetPixel(400,300).A != 0 || b.GetPixel(400,0).A < 200 || b.GetPixel(400,89).A > 1) throw new Exception("Feather validation failed");
                int previous = 256;
                for (int y=0;y<100;y++) { int a=b.GetPixel(400,y).A; if(a>previous) throw new Exception("Non-monotonic feather"); previous=a; }
                b.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"feather-test.png"));
            }
            return;
        }
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        if (args.Length > 0 && args[0] == "--preview") { using (var panel = new ControlsWindow(true)) { panel.Show(); Application.DoEvents(); using (var preview = new Bitmap(panel.ClientSize.Width, panel.ClientSize.Height)) { panel.DrawToBitmap(preview, new Rectangle(Point.Empty, panel.ClientSize)); preview.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LUMA-preview.png")); } } return; } using(var instance=new SingleInstance()) { if(!instance.Acquired) { MessageBox.Show("LUMA is already running.\n\nUse the LUMA icon in the system tray to open its controls.\nA second instance will not be opened.","LUMA is already running",MessageBoxButtons.OK,MessageBoxIcon.Information); return; } Application.Run(new ControlsWindow(false)); }
    }
}

static class Glow {
    internal static double Falloff(int distance, int width) {
        double t=Math.Min(1.0,(double)distance/width);
        return 1-t*t*(3-2*t);
    }
    internal static Bitmap Render(int w,int h,int width,double brightness,Color color) {
        Bitmap b=new Bitmap(w,h,PixelFormat.Format32bppPArgb);
        BitmapData data=b.LockBits(new Rectangle(0,0,w,h),ImageLockMode.WriteOnly,b.PixelFormat);
        try {
            byte[] pixels=new byte[data.Stride*h];
            double[] xs=new double[w];
            for(int x=0;x<w;x++) xs[x]=Falloff(Math.Min(x,w-1-x),width);
            for(int y=0;y<h;y++) {
                double fy=Falloff(Math.Min(y,h-1-y),width);
                for(int x=0;x<w;x++) {
                    // Smooth union avoids diagonal seams where adjacent edges meet.
                    double alpha=brightness*(1-(1-xs[x])*(1-fy));
                    int a=(int)Math.Round(255*alpha), i=y*data.Stride+x*4;
                    pixels[i]=(byte)((color.B*a+127)/255);
                    pixels[i+1]=(byte)((color.G*a+127)/255);
                    pixels[i+2]=(byte)((color.R*a+127)/255);
                    pixels[i+3]=(byte)a;
                }
            }
            Marshal.Copy(pixels,0,data.Scan0,pixels.Length);
        } finally { b.UnlockBits(data); }
        return b;
    }
}

class Overlay : Form {
    bool displayTaskbar;
    int lastWidth=120,lastBrightness=75; Color lastColor=Color.White;
    internal Rectangle RenderBounds { get; private set; }
    internal Overlay() { FormBorderStyle=FormBorderStyle.None; ShowInTaskbar=false; TopMost=true; }
    protected override bool ShowWithoutActivation { get { return true; } }
    protected override CreateParams CreateParams { get { var p=base.CreateParams; p.ExStyle|=0x80000|0x20|0x80|0x08000000; return p; } }
    internal void UpdateGlow(int width,int brightness,Color color) {
        lastWidth=width;lastBrightness=brightness;lastColor=color;
        Rectangle r=LightBounds(Screen.PrimaryScreen.Bounds,Screen.PrimaryScreen.WorkingArea,displayTaskbar);
        RenderBounds=r;
        using(Bitmap b=Glow.Render(r.Width,r.Height,Math.Min(width,Math.Min(r.Width,r.Height)/2),brightness/100.0,color)) {
            IntPtr screen=Native.GetDC(IntPtr.Zero), memory=Native.CreateCompatibleDC(screen), bitmap=IntPtr.Zero, old=IntPtr.Zero;
            try {
                bitmap=b.GetHbitmap(Color.FromArgb(0)); old=Native.SelectObject(memory,bitmap);
                Native.Point dest=new Native.Point(r.X,r.Y), src=new Native.Point(0,0);
                Native.Size size=new Native.Size(r.Width,r.Height);
                Native.Blend blend=new Native.Blend { Op=0, Flags=0, Alpha=255, Format=1 };
                if(!Native.UpdateLayeredWindow(Handle,screen,ref dest,ref size,memory,ref src,0,ref blend,2)) throw new System.ComponentModel.Win32Exception();
            } finally { if(old!=IntPtr.Zero) Native.SelectObject(memory,old); if(bitmap!=IntPtr.Zero) Native.DeleteObject(bitmap); Native.DeleteDC(memory); Native.ReleaseDC(IntPtr.Zero,screen); }
        }
    }
    internal static Rectangle LightBounds(Rectangle monitor,Rectangle workingArea,bool showTaskbar) {
        if(!showTaskbar)return monitor;
        Rectangle safe=Rectangle.Intersect(monitor,workingArea);
        if(safe.Width>0&&safe.Height>0&&safe!=monitor)return safe;
        // Auto-hide has no reserved working area. Leave the screen edges free
        // so this topmost overlay is not a fullscreen window blocking reveal.
        safe=monitor;safe.Inflate(-1,-1);return safe;
    }
    internal void SetTaskbarVisibility(bool value) {
        Rectangle expected=LightBounds(Screen.PrimaryScreen.Bounds,Screen.PrimaryScreen.WorkingArea,value);
        if(displayTaskbar==value&&RenderBounds==expected)return;
        displayTaskbar=value;
        Region old=Region;Region=null;if(old!=null)old.Dispose();
        UpdateGlow(lastWidth,lastBrightness,lastColor);
    }
}

static class Native {
    [StructLayout(LayoutKind.Sequential)] internal struct Point { internal int X,Y; internal Point(int x,int y){X=x;Y=y;} }
    [StructLayout(LayoutKind.Sequential)] internal struct Size { internal int W,H; internal Size(int w,int h){W=w;H=h;} }
    [StructLayout(LayoutKind.Sequential,Pack=1)] internal struct Blend { internal byte Op,Flags,Alpha,Format; }
    [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] internal static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] internal static extern int ReleaseDC(IntPtr hwnd,IntPtr dc);
    [DllImport("gdi32.dll")] internal static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] internal static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] internal static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
    [DllImport("gdi32.dll")] internal static extern bool DeleteObject(IntPtr obj);
    [DllImport("user32.dll",SetLastError=true)] internal static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr dc,ref Point dest,ref Size size,IntPtr source,ref Point origin,int key,ref Blend blend,int flags);
    [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr hwnd,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr hwnd,int id);
}



