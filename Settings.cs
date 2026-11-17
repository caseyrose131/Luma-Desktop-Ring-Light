using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

sealed class LayerPreferences {
    internal bool DisplayTaskbar=true;
    internal HashSet<string> Apps=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"layers.txt");
    internal LayerPreferences(string file=null){if(file!=null)path=file;try{foreach(string line in File.ReadAllLines(path)){if(line=="taskbar=true")DisplayTaskbar=true;else if(line=="taskbar=false")DisplayTaskbar=false;else if(line.StartsWith("app="))Apps.Add(line.Substring(4));}}catch(IOException){}catch(UnauthorizedAccessException){}}
    internal void Save(){try{File.WriteAllLines(path,new[]{"taskbar="+DisplayTaskbar.ToString().ToLowerInvariant()}.Concat(Apps.OrderBy(x=>x).Select(x=>"app="+x)).ToArray());}catch(IOException){}catch(UnauthorizedAccessException){}}
}

sealed class DesktopWindow {
    internal IntPtr Handle; internal string App,Title; internal Rectangle Bounds; internal bool Taskbar,Own;
}

static class DesktopWindows {
    delegate bool EnumProc(IntPtr hwnd,IntPtr arg);
    [StructLayout(LayoutKind.Sequential)] struct Rect {internal int Left,Top,Right,Bottom;internal Rectangle Rectangle{get{return Rectangle.FromLTRB(Left,Top,Right,Bottom);}}}
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc callback,IntPtr arg);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int GetWindowText(IntPtr hwnd,StringBuilder text,int count);
    [DllImport("user32.dll")] static extern int GetClassName(IntPtr hwnd,StringBuilder text,int count);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd,out Rect rect);
    [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr hwnd,int attr,out Rect rect,int size);
    [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr hwnd,int attr,out int value,int size);
    static int ownId=Process.GetCurrentProcess().Id;
    internal static List<DesktopWindow> Read(IntPtr overlay,bool includeMinimized=false){
        var windows=new List<DesktopWindow>();var names=new Dictionary<uint,string>();
        EnumWindows((hwnd,arg)=>{
            if(hwnd==overlay||!IsWindowVisible(hwnd)||(!includeMinimized&&IsIconic(hwnd)))return true;
            int cloaked; if(DwmGetWindowAttribute(hwnd,14,out cloaked,4)==0&&cloaked!=0)return true;
            var cls=new StringBuilder(256);GetClassName(hwnd,cls,cls.Capacity);string className=cls.ToString();
            if(className=="Progman"||className=="WorkerW")return true;
            var title=new StringBuilder(1024);GetWindowText(hwnd,title,title.Capacity);
            bool taskbar=className=="Shell_TrayWnd"||className=="Shell_SecondaryTrayWnd";
            Rect rect;
            // The shell taskbar's DWM frame bounds can be empty or stale.
            if(taskbar){if(!GetWindowRect(hwnd,out rect))return true;}
            else if(DwmGetWindowAttribute(hwnd,9,out rect,16)!=0&&!GetWindowRect(hwnd,out rect))return true;
            uint pid;GetWindowThreadProcessId(hwnd,out pid);string name;
            if(!names.TryGetValue(pid,out name)){try{using(var process=Process.GetProcessById((int)pid))name=process.ProcessName;}catch{name="";}names[pid]=name;}
            windows.Add(new DesktopWindow{Handle=hwnd,App=name,Title=title.ToString(),Bounds=rect.Rectangle,Taskbar=taskbar,Own=pid==ownId});return true;
        },IntPtr.Zero);return windows;
    }
    // Remove only visible portions of selected windows; ordinary windows above
    // them still receive the ring. Never alter another app's topmost state.
    internal static Region BuildMask(Rectangle monitor,IEnumerable<DesktopWindow> windows,LayerPreferences preferences){
        Region mask=new Region(monitor);using(Region covered=new Region()){
            covered.MakeEmpty();foreach(var window in windows){
                Rectangle bounds=Rectangle.Intersect(monitor,window.Bounds);if(bounds.IsEmpty)continue;
                if(window.Taskbar&&preferences.DisplayTaskbar){mask.Exclude(bounds);}
                else if(window.Own||preferences.Apps.Contains(window.App)){
                    using(Region visible=new Region(bounds)){visible.Exclude(covered);mask.Exclude(visible);}
                }
                covered.Union(bounds);
            }
        }
        mask.Translate(-monitor.X,-monitor.Y);return mask;
    }
}

sealed class LayerController : IDisposable {
    Overlay overlay;LayerPreferences preferences;Timer timer=new Timer();string signature;
    internal LayerController(Overlay target,LayerPreferences settings){overlay=target;preferences=settings;timer.Interval=200;timer.Tick+=(s,e)=>Refresh();timer.Start();}
    internal void Refresh(){if(!overlay.Visible)return;overlay.SetTaskbarVisibility(preferences.DisplayTaskbar);var windows=DesktopWindows.Read(overlay.Handle);Rectangle monitor=overlay.RenderBounds;
        string current=monitor.ToString()+preferences.DisplayTaskbar+string.Join(";",preferences.Apps.OrderBy(x=>x))+string.Join(";",windows.Select(w=>w.Handle+":"+w.Bounds+":"+w.App+":"+w.Own+":"+w.Taskbar));
        if(current==signature)return;signature=current;Region old=overlay.Region;overlay.Region=DesktopWindows.BuildMask(monitor,windows,preferences);if(old!=null)old.Dispose();
    }
    public void Dispose(){timer.Dispose();}
}

sealed class SettingsWindow : Form {
    CheckBox taskbar; LayerPreferences preferences;Action changed;CheckedListBox apps;Label empty;Timer refresh=new Timer();bool filling;string signature;
    sealed class AppItem {internal string Key,Name;public override string ToString(){return Name;}}
    internal SettingsWindow(LayerPreferences prefs,Action onChanged,Icon icon){
        preferences=prefs;changed=onChanged;Text="LUMA / Settings";Icon=icon;ClientSize=new Size(480,550);FormBorderStyle=FormBorderStyle.None;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
        BackColor=Design.Background;ForeColor=Design.Ink;Font=Design.Type(10);AutoScaleMode=AutoScaleMode.None;
        AddLabel("SETTINGS",24,22,18,true);AddLabel("CONTROL WHAT SITS ABOVE THE LIGHT",24,57,9,false);
        taskbar=new CheckBox{Text="Display taskbar above the ring",Checked=preferences.DisplayTaskbar,Location=new Point(24,96),Size=new Size(425,28),FlatStyle=FlatStyle.Flat,ForeColor=Design.Ink};Controls.Add(taskbar);
        taskbar.CheckedChanged+=(s,e)=>{if(preferences.DisplayTaskbar==taskbar.Checked)return;preferences.DisplayTaskbar=taskbar.Checked;Save();};
        AddLabel("APPS ABOVE THE RING",24,158,11,true);AddLabel("Tick an app to keep its windows clear of the glow.",24,187,10,false);
        apps=new CheckedListBox{Location=new Point(24,221),Size=new Size(432,230),BackColor=Design.Surface,ForeColor=Design.Ink,BorderStyle=BorderStyle.FixedSingle,CheckOnClick=true,IntegralHeight=false,Font=Design.Type(11),AccessibleName="Apps above the ring",HorizontalScrollbar=true};Controls.Add(apps);
        apps.ItemCheck+=(s,e)=>{if(filling)return;var app=(AppItem)apps.Items[e.Index];if(e.NewValue==CheckState.Checked)preferences.Apps.Add(app.Key);else preferences.Apps.Remove(app.Key);Save();};
        empty=AddLabel("",24,465,9,false);
        var done=new TechButton{Text="DONE",Primary=true,Location=new Point(324,496),Size=new Size(132,36)};Controls.Add(done);done.Click+=(s,e)=>Close();
        AddLabel("AUTO-SAVED / LIVE APP LIST",24,508,8,false);
        RefreshApps();refresh.Interval=1500;refresh.Tick+=(s,e)=>RefreshApps();refresh.Start();
    }
    Label AddLabel(string text,int x,int y,int size,bool bold){var l=new Label{Text=text,Location=new Point(x,y),AutoSize=true,Font=Design.Type(size,bold),ForeColor=bold?Design.Ink:Design.Muted};Controls.Add(l);return l;}
    internal void SyncTaskbar(){taskbar.Checked=preferences.DisplayTaskbar;} void Save(){preferences.Save();changed();}
    internal void RefreshApps(){
        var running=DesktopWindows.Read(IntPtr.Zero,true).Where(w=>!w.Own&&!w.Taskbar&&w.Title.Length>0&&w.App.Length>0).GroupBy(w=>w.App,StringComparer.OrdinalIgnoreCase).OrderBy(g=>g.Key).ToList();
        var items=running.Select(g=>new AppItem{Key=g.Key,Name=g.Key+"  /  "+g.First().Title}).ToList();
        foreach(string saved in preferences.Apps.OrderBy(x=>x))if(!items.Any(i=>string.Equals(i.Key,saved,StringComparison.OrdinalIgnoreCase)))items.Add(new AppItem{Key=saved,Name=saved+"  /  not running"});
        string next=string.Join("\n",items.Select(i=>i.Key+"|"+i.Name));if(next==signature)return;signature=next;
        string selected=apps.SelectedItem==null?null:((AppItem)apps.SelectedItem).Key;int top=apps.TopIndex;filling=true;
        try{apps.BeginUpdate();apps.Items.Clear();foreach(var item in items){int index=apps.Items.Add(item,preferences.Apps.Contains(item.Key));if(item.Key==selected)apps.SelectedIndex=index;}if(apps.Items.Count>0)apps.TopIndex=Math.Min(top,apps.Items.Count-1);}
        finally{apps.EndUpdate();filling=false;}
        empty.Text=running.Count==0?"No other open apps. This list updates automatically.":running.Count+" RUNNING APPS / REFRESHES AUTOMATICALLY";
    }
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(Pen p=new Pen(Design.Line)){e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);e.Graphics.DrawLine(p,24,80,456,80);}Design.Logo(e.Graphics,new Rectangle(420,24,30,30),Design.Accent);}
    protected override void Dispose(bool disposing){if(disposing)refresh.Dispose();base.Dispose(disposing);}
}


