using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

static class Design {
    internal static Color Background=Color.FromArgb(12,16,21), Surface=Color.FromArgb(19,25,32), Line=Color.FromArgb(40,52,63), Muted=Color.FromArgb(130,148,162), Ink=Color.FromArgb(232,241,246), Accent=Color.FromArgb(132,255,203);
    internal static Font Type(float size, bool bold=false){return new Font("Bahnschrift",size,bold?FontStyle.Bold:FontStyle.Regular);}
    internal static void Logo(Graphics g, Rectangle r, Color color) {
        g.SmoothingMode=SmoothingMode.AntiAlias;
        float u=r.Width/64f, x=r.X,y=r.Y;
        using(Pen p=new Pen(color,7*u)) {
            p.StartCap=LineCap.Square;p.EndCap=LineCap.Square;p.LineJoin=LineJoin.Bevel;
            g.DrawLines(p,new[]{new PointF(x+18*u,y+9*u),new PointF(x+9*u,y+18*u),new PointF(x+9*u,y+46*u),new PointF(x+18*u,y+55*u),new PointF(x+46*u,y+55*u),new PointF(x+55*u,y+46*u),new PointF(x+55*u,y+18*u),new PointF(x+46*u,y+9*u)});
            g.DrawLine(p,x+32*u,y+5*u,x+32*u,y+26*u);
        }
    }
}

class TechButton : Button {
    internal bool Selected, Primary; bool hover;
    internal TechButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);Cursor=Cursors.Hand;Font=Design.Type(10,true);}
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e){
        Color bg=Primary?Design.Accent:(hover?Color.FromArgb(34,48,54):Design.Surface);
        e.Graphics.Clear(bg);
        using(Pen p=new Pen(Selected||hover||Focused?Design.Accent:Design.Line))e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);
        TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Primary?Design.Background:(Selected?Design.Accent:Design.Ink),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        if(Focused) ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(4,4,Width-8,Height-8));
    }
}

class TechSlider : Control {
    internal int Minimum,Maximum; int current; bool dragging;
    internal event EventHandler ValueChanged;
    internal int Value {get{return current;}set{int next=Math.Max(Minimum,Math.Min(Maximum,value));if(next==current)return;current=next;Invalidate();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
    internal TechSlider(){SetStyle(ControlStyles.Selectable|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);TabStop=true;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.Slider;}
    protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Design.Background);int x=10+(int)((Width-20)*(Value-Minimum)/(double)(Maximum-Minimum));
        using(Pen rail=new Pen(Design.Line,3))e.Graphics.DrawLine(rail,10,Height/2,Width-10,Height/2);
        using(Pen fill=new Pen(Design.Accent,3))e.Graphics.DrawLine(fill,10,Height/2,x,Height/2);
        using(Brush b=new SolidBrush(Design.Accent))e.Graphics.FillRectangle(b,x-5,Height/2-8,10,16);
        if(Focused)using(Pen p=new Pen(Design.Muted))e.Graphics.DrawRectangle(p,x-8,Height/2-11,16,22);
    }
    void MoveValue(int x){Value=Minimum+(int)Math.Round(Math.Max(0,Math.Min(1,(x-10)/(double)(Width-20)))*(Maximum-Minimum));}
    protected override void OnMouseDown(MouseEventArgs e){if(e.Button==MouseButtons.Left){Focus();dragging=true;Capture=true;MoveValue(e.X);}base.OnMouseDown(e);}
    protected override void OnMouseMove(MouseEventArgs e){if(dragging)MoveValue(e.X);base.OnMouseMove(e);}
    protected override void OnMouseUp(MouseEventArgs e){dragging=false;Capture=false;base.OnMouseUp(e);}
    protected override void OnMouseCaptureChanged(EventArgs e){dragging=false;base.OnMouseCaptureChanged(e);}
    protected override bool IsInputKey(Keys key){return key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down||key==Keys.Home||key==Keys.End||base.IsInputKey(key);}
    protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Down)Value--;if(e.KeyCode==Keys.Right||e.KeyCode==Keys.Up)Value++;if(e.KeyCode==Keys.Home)Value=Minimum;if(e.KeyCode==Keys.End)Value=Maximum;base.OnKeyDown(e);}
    protected override void OnGotFocus(EventArgs e){Invalidate();base.OnGotFocus(e);} protected override void OnLostFocus(EventArgs e){Invalidate();base.OnLostFocus(e);}
}

class LightPreview : Control {
    internal Color Light=Color.White; internal int Brightness=75,Feather=120; internal bool Active=true; internal float Phase;
    internal LightPreview(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Design.Surface);g.SmoothingMode=SmoothingMode.AntiAlias;
        using(Pen p=new Pen(Color.FromArgb(25,36,44)))for(int x=16;x<Width;x+=24)for(int y=14;y<Height;y+=24)g.DrawLine(p,x,y,x+1,y);
        Rectangle screen=new Rectangle(54,28,Width-108,108);
        using(Brush b=new SolidBrush(Design.Background))g.FillRectangle(b,screen);
        if(Active)using(Bitmap glow=Glow.Render(screen.Width,screen.Height,Math.Max(6,Feather/6),Brightness/100.0,Light))g.DrawImageUnscaled(glow,screen.Location);
        using(Pen p=new Pen(Design.Line)) {g.DrawRectangle(p,screen);g.DrawLine(p,Width/2,138,Width/2,149);g.DrawLine(p,Width/2-23,150,Width/2+23,150);}
        Design.Logo(g,new Rectangle(Width/2-18,64,36,36),Active?Design.Accent:Design.Muted);
        if(Active){int perimeter=2*(screen.Width+screen.Height);float d=Phase*perimeter;PointF point;
            if(d<screen.Width)point=new PointF(screen.X+d,screen.Y);else if((d-=screen.Width)<screen.Height)point=new PointF(screen.Right,screen.Y+d);else if((d-=screen.Height)<screen.Width)point=new PointF(screen.Right-d,screen.Bottom);else point=new PointF(screen.X,screen.Bottom-(d-screen.Width));
            for(int r=10;r>=2;r-=2)using(Brush b=new SolidBrush(Color.FromArgb(25,Design.Accent)))g.FillEllipse(b,point.X-r,point.Y-r,2*r,2*r);
            using(Brush b=new SolidBrush(Design.Ink))g.FillRectangle(b,point.X-2,point.Y-2,4,4);
        }
    }
}

class ControlsWindow : Form {
    LayerPreferences layerPreferences=new LayerPreferences(); CheckBox taskbarToggle; LayerController layers; SettingsWindow settingsPanel; Overlay overlay; NotifyIcon tray; TechSlider brightness,width; Label brightnessValue,widthValue,status; TechButton toggle,warm,daylight,custom; LightPreview preview;
    Color light=Color.FromArgb(255,244,229); bool enabled=true; readonly bool previewOnly;
    Timer debounce=new Timer(), animation=new Timer(); System.Diagnostics.Stopwatch clock=System.Diagnostics.Stopwatch.StartNew();
    string settings=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings.txt");
    internal ControlsWindow(bool renderOnly){
        previewOnly=renderOnly;Text="LUMA / Desktop light";ClientSize=new Size(480,694);AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;
        BackColor=Design.Background;ForeColor=Design.Ink;Font=Design.Type(10);DoubleBuffered=true;
        string icon=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"LUMA.ico");if(File.Exists(icon))Icon=new Icon(icon);
        Label brand=Label("L U M A",86,25,22,true);brand.AccessibleName="LUMA";
        Label("DESKTOP LIGHT",88,63,8,false).ForeColor=Design.Muted; var minimize=Button("_",374,28,32); minimize.Size=new Size(32,28); minimize.AccessibleName="Minimize"; minimize.Click+=(s,e)=>WindowState=FormWindowState.Minimized; var close=Button("X",414,28,36); close.Size=new Size(36,28); close.AccessibleName="Quit LUMA"; close.Click+=(s,e)=>Close();
        Label("01 / PRIMARY DISPLAY",30,109,9,false).ForeColor=Design.Muted;
        preview=new LightPreview {Location=new Point(30,133),Size=new Size(420,174),AccessibleName="Live light preview"};Controls.Add(preview);
        Label("INTENSITY",30,335,10,true);brightnessValue=Label("",351,335,11,true);brightness=Slider(359,5,100,75,"Intensity");
        Label("EDGE SOFTNESS",30,409,10,true);widthValue=Label("",351,409,11,true);width=Slider(433,20,400,120,"Edge softness");
        Label("LIGHT COLOR",30,483,9,true).ForeColor=Design.Muted;
        warm=Button("WARM",30,507,132);daylight=Button("DAYLIGHT",174,507,132);custom=Button("CUSTOM +",318,507,132);
        warm.Click+=(s,e)=>{light=Color.FromArgb(255,244,229);Queue();};daylight.Click+=(s,e)=>{light=Color.White;Queue();};
        custom.Click+=(s,e)=>{using(ColorDialog d=new ColorDialog{Color=light,FullOpen=true})if(d.ShowDialog(this)==DialogResult.OK){light=d.Color;Queue();}};
        taskbarToggle=new CheckBox {Text="Display taskbar above the ring",Checked=layerPreferences.DisplayTaskbar,Location=new Point(30,560),Size=new Size(420,28),FlatStyle=FlatStyle.Flat,ForeColor=Design.Ink};Controls.Add(taskbarToggle);taskbarToggle.CheckedChanged+=(s,e)=>{if(layerPreferences.DisplayTaskbar==taskbarToggle.Checked)return;layerPreferences.DisplayTaskbar=taskbarToggle.Checked;if(!previewOnly)layerPreferences.Save();if(settingsPanel!=null&&!settingsPanel.IsDisposed)settingsPanel.SyncTaskbar();if(layers!=null)layers.Refresh();}; toggle=Button("",30,607,276);toggle.Primary=true;toggle.Click+=(s,e)=>Toggle();
        Button("HIDE PANEL",318,607,132).Click+=(s,e)=>Hide();
        status=Label("CTRL + ALT + L  /  TOGGLE LIGHT",30,665,8,false);status.ForeColor=Design.Muted; var settingsButton=Button("SETTINGS",350,657,100);settingsButton.Size=new Size(100,27);settingsButton.Click+=(s,e)=>OpenSettings();
        LoadSettings();UpdateLabels();
        debounce.Interval=80;debounce.Tick+=(s,e)=>{debounce.Stop();Apply();};brightness.ValueChanged+=(s,e)=>Queue();width.ValueChanged+=(s,e)=>Queue();
        animation.Interval=33;animation.Tick+=(s,e)=>{if(Visible&&WindowState!=FormWindowState.Minimized&&enabled&&SystemInformation.IsMenuAnimationEnabled){preview.Phase=(float)((clock.Elapsed.TotalSeconds%8)/8);preview.Invalidate();}};
        if(!previewOnly){overlay=new Overlay();ContextMenuStrip menu=new ContextMenuStrip();menu.Items.Add("Open LUMA",null,(s,e)=>ShowPanel());menu.Items.Add("Toggle light",null,(s,e)=>Toggle());menu.Items.Add("Quit LUMA",null,(s,e)=>Close());
            tray=new NotifyIcon {Icon=Icon,Text="LUMA / Desktop light",Visible=true,ContextMenuStrip=menu};tray.DoubleClick+=(s,e)=>ShowPanel();
            Shown+=(s,e)=>{overlay.Show();Apply();layers=new LayerController(overlay,layerPreferences);layers.Refresh();animation.Start();if(!Native.RegisterHotKey(Handle,1,0x0002|0x0004|0x4000,(uint)Keys.L))status.Text="TRAY ICON / TOGGLE LIGHT";};SystemEvents.DisplaySettingsChanged+=DisplayChanged;
        }
    }
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left && e.Y<94){ReleaseCapture();SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero);}} [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool ReleaseCapture(); [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hwnd,int msg,IntPtr w,IntPtr l); protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(Pen border=new Pen(Design.Line))e.Graphics.DrawRectangle(border,0,0,Width-1,Height-1);Design.Logo(e.Graphics,new Rectangle(30,29,42,42),Design.Accent);using(Pen p=new Pen(Design.Line))e.Graphics.DrawLine(p,30,94,450,94);using(Brush b=new SolidBrush(enabled?Design.Accent:Design.Muted))e.Graphics.FillRectangle(b,348,39,6,6);}
    Label Label(string text,int x,int y,float size,bool bold){var l=new Label{Text=text,Location=new Point(x,y),AutoSize=true,Font=Design.Type(size,bold),ForeColor=Design.Ink};Controls.Add(l);return l;}
    TechSlider Slider(int y,int min,int max,int value,string name){var t=new TechSlider {Minimum=min,Maximum=max,Value=value,Location=new Point(23,y),Size=new Size(434,32),AccessibleName=name};Controls.Add(t);return t;}
    TechButton Button(string text,int x,int y,int width){var b=new TechButton {Text=text,Location=new Point(x,y),Size=new Size(width,40)};Controls.Add(b);return b;}
    internal void OpenSettings(){if(settingsPanel!=null&&!settingsPanel.IsDisposed){settingsPanel.Activate();return;}settingsPanel=new SettingsWindow(layerPreferences,()=>{taskbarToggle.Checked=layerPreferences.DisplayTaskbar;if(layers!=null)layers.Refresh();},Icon);settingsPanel.Show(this);} void ShowPanel(){Show();WindowState=FormWindowState.Normal;Activate();}
    void Queue(){UpdateLabels();debounce.Stop();debounce.Start();}
    void UpdateLabels(){brightnessValue.Text=brightness.Value.ToString("00")+" %";widthValue.Text=width.Value+" PX";toggle.Text=enabled?"LIGHT ON  /  PAUSE":"LIGHT OFF  /  RESUME";toggle.Primary=enabled;
        warm.Selected=light.ToArgb()==Color.FromArgb(255,244,229).ToArgb();daylight.Selected=light.ToArgb()==Color.White.ToArgb();custom.Selected=!warm.Selected&&!daylight.Selected;
        warm.Invalidate();daylight.Invalidate();custom.Invalidate();toggle.Invalidate();preview.Light=light;preview.Brightness=brightness.Value;preview.Feather=width.Value;preview.Active=enabled;preview.Invalidate();Invalidate();}
    void Toggle(){enabled=!enabled;Apply();}
    void Apply(){UpdateLabels();if(previewOnly)return;if(enabled){overlay.UpdateGlow(width.Value,brightness.Value,light);overlay.Show();if(layers!=null)layers.Refresh();}else overlay.Hide();SaveSettings();}
    void DisplayChanged(object sender,EventArgs e){if(!IsDisposed&&IsHandleCreated)BeginInvoke(new Action(()=>Apply()));}
    void LoadSettings(){try{string[] p=File.ReadAllLines(settings);brightness.Value=int.Parse(p[0]);width.Value=int.Parse(p[1]);light=Color.FromArgb(int.Parse(p[2]));}catch{}}
    void SaveSettings(){try{File.WriteAllLines(settings,new[]{brightness.Value.ToString(),width.Value.ToString(),light.ToArgb().ToString()});}catch(IOException){}catch(UnauthorizedAccessException){}}
    protected override void WndProc(ref Message m){if(m.Msg==0x0312&&m.WParam.ToInt32()==1)Toggle();base.WndProc(ref m);}
    protected override void Dispose(bool disposing){if(disposing){if(!previewOnly){SystemEvents.DisplaySettingsChanged-=DisplayChanged;if(IsHandleCreated)Native.UnregisterHotKey(Handle,1);}if(settingsPanel!=null)settingsPanel.Dispose();if(layers!=null)layers.Dispose();debounce.Dispose();animation.Dispose();if(tray!=null)tray.Dispose();if(overlay!=null)overlay.Dispose();}base.Dispose(disposing);}
}




