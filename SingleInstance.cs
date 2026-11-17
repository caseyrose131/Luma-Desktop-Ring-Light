using System;
using System.Threading;
using System.Security.Principal;

// Shared across installation folders, scoped to this Windows user and session.
sealed class SingleInstance : IDisposable {
    Mutex mutex;
    internal bool Acquired { get; private set; }
    internal SingleInstance(string name=null) {
        if(name==null) {
            using(WindowsIdentity user=WindowsIdentity.GetCurrent())
                name="Local\\LUMA.DesktopLight.Instance."+user.User.Value;
        }
        mutex=new Mutex(false,name);
        try { Acquired=mutex.WaitOne(0); }
        catch(AbandonedMutexException) { Acquired=true; }
    }
    public void Dispose() {
        if(mutex==null)return;
        if(Acquired){mutex.ReleaseMutex();Acquired=false;}
        mutex.Dispose();mutex=null;
    }
}
