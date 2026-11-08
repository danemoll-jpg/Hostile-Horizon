"""Fix 6.2: log a real HOTAS's HID axes as Windows reports them (no window, no sound, no focus).

Prints the device's HID descriptor (every value: usage page / usage, the name Unity's Input System gives it, logical range, bits)
and then every change of every value and button while it runs, so the throttle lever can be found with data.

    python Tools/hotas_hid_log.py [seconds] [--vid 044f] [--pid b67c] [--out file]
"""
import ctypes as C
import ctypes.wintypes as W
import os, sys, time, argparse

hid = C.WinDLL("hid.dll"); setup = C.WinDLL("setupapi.dll"); k32 = C.WinDLL("kernel32.dll", use_last_error=True)

class GUID(C.Structure):
    _fields_ = [("a", W.DWORD), ("b", W.WORD), ("c", W.WORD), ("d", C.c_ubyte * 8)]
class SP_DEVICE_INTERFACE_DATA(C.Structure):
    _fields_ = [("cbSize", W.DWORD), ("guid", GUID), ("flags", W.DWORD), ("res", C.c_void_p)]
class HIDD_ATTRIBUTES(C.Structure):
    _fields_ = [("Size", W.ULONG), ("VendorID", W.USHORT), ("ProductID", W.USHORT), ("Version", W.USHORT)]
class HIDP_CAPS(C.Structure):
    _fields_ = [("Usage", W.USHORT), ("UsagePage", W.USHORT), ("InputReportByteLength", W.USHORT), ("OutputReportByteLength", W.USHORT),
                ("FeatureReportByteLength", W.USHORT), ("Reserved", W.USHORT * 17), ("NumberLinkCollectionNodes", W.USHORT),
                ("NumberInputButtonCaps", W.USHORT), ("NumberInputValueCaps", W.USHORT), ("NumberInputDataIndices", W.USHORT),
                ("NumberOutputButtonCaps", W.USHORT), ("NumberOutputValueCaps", W.USHORT), ("NumberOutputDataIndices", W.USHORT),
                ("NumberFeatureButtonCaps", W.USHORT), ("NumberFeatureValueCaps", W.USHORT), ("NumberFeatureDataIndices", W.USHORT)]
class RANGE(C.Structure):
    _fields_ = [("UsageMin", W.USHORT), ("UsageMax", W.USHORT), ("StringMin", W.USHORT), ("StringMax", W.USHORT),
                ("DesignatorMin", W.USHORT), ("DesignatorMax", W.USHORT), ("DataIndexMin", W.USHORT), ("DataIndexMax", W.USHORT)]
class U(C.Union):
    _fields_ = [("Range", RANGE)]
class HIDP_VALUE_CAPS(C.Structure):
    _fields_ = [("UsagePage", W.USHORT), ("ReportID", C.c_ubyte), ("IsAlias", C.c_ubyte), ("BitField", W.USHORT), ("LinkCollection", W.USHORT),
                ("LinkUsage", W.USHORT), ("LinkUsagePage", W.USHORT), ("IsRange", C.c_ubyte), ("IsStringRange", C.c_ubyte),
                ("IsDesignatorRange", C.c_ubyte), ("IsAbsolute", C.c_ubyte), ("HasNull", C.c_ubyte), ("Reserved", C.c_ubyte),
                ("BitSize", W.USHORT), ("ReportCount", W.USHORT), ("Reserved2", W.USHORT * 5), ("UnitsExp", W.ULONG), ("Units", W.ULONG),
                ("LogicalMin", C.c_long), ("LogicalMax", C.c_long), ("PhysicalMin", C.c_long), ("PhysicalMax", C.c_long), ("u", U)]
class HIDP_BUTTON_CAPS(C.Structure):
    _fields_ = [("UsagePage", W.USHORT), ("ReportID", C.c_ubyte), ("IsAlias", C.c_ubyte), ("BitField", W.USHORT), ("LinkCollection", W.USHORT),
                ("LinkUsage", W.USHORT), ("LinkUsagePage", W.USHORT), ("IsRange", C.c_ubyte), ("IsStringRange", C.c_ubyte),
                ("IsDesignatorRange", C.c_ubyte), ("IsAbsolute", C.c_ubyte), ("ReportCount", W.USHORT), ("Reserved2", W.USHORT),
                ("Reserved", W.ULONG * 9), ("u", U)]

GD = {0x30: "x", 0x31: "y", 0x32: "z", 0x33: "rx", 0x34: "ry", 0x35: "rz", 0x36: "slider", 0x37: "dial", 0x38: "wheel", 0x39: "hat"}
UNITY_AXES = {0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38}

def name(page, usage):
    if page == 1:
        n = GD.get(usage, f"GenericDesktop 0x{usage:02x}")
        unity = ("stick/" + n if usage in (0x30, 0x31) else n) if usage in UNITY_AXES or usage == 0x39 else "(not used by Unity)"
        return n, unity
    if page == 2: return {0xBA: "rudder", 0xBB: "throttle", 0xC4: "accelerator", 0xC5: "brake"}.get(usage, f"Simulation 0x{usage:02x}"), "(not used by Unity: Simulation page)"
    if page == 9: return f"button {usage}", "trigger" if usage == 1 else f"button{usage}"
    return f"page 0x{page:02x} usage 0x{usage:02x}", "(not used by Unity)"

def find(vid, pid):
    g = GUID(); hid.HidD_GetHidGuid(C.byref(g))
    setup.SetupDiGetClassDevsW.restype = C.c_void_p
    info = setup.SetupDiGetClassDevsW(C.byref(g), None, None, 0x12)
    i = 0; out = []
    while True:
        d = SP_DEVICE_INTERFACE_DATA(); d.cbSize = C.sizeof(d)
        if not setup.SetupDiEnumDeviceInterfaces(C.c_void_p(info), None, C.byref(g), i, C.byref(d)): break
        need = W.DWORD(); setup.SetupDiGetDeviceInterfaceDetailW(C.c_void_p(info), C.byref(d), None, 0, C.byref(need), None)
        buf = C.create_string_buffer(need.value); C.cast(buf, C.POINTER(W.DWORD))[0] = 8 if C.sizeof(C.c_void_p) == 8 else 6
        if setup.SetupDiGetDeviceInterfaceDetailW(C.c_void_p(info), C.byref(d), buf, need, None, None):
            path = C.wstring_at(C.addressof(buf) + 4)
            if f"vid_{vid}" in path.lower() and f"pid_{pid}" in path.lower(): out.append(path)
        i += 1
    return out

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("seconds", nargs="?", type=float, default=60); ap.add_argument("--vid", default="044f")
    ap.add_argument("--pid", default="b67c"); ap.add_argument("--out"); a = ap.parse_args()
    out = open(a.out, "w", encoding="utf-8") if a.out else None
    def say(s):
        print(s, flush=True)
        if out: out.write(s + "\n"); out.flush()
    k32.CreateFileW.restype = W.HANDLE
    paths = find(a.vid, a.pid)
    if not paths: say(f"no HID device vid_{a.vid} pid_{a.pid}"); return
    for p in paths: say("device " + p)
    h = k32.CreateFileW(paths[0], 0x80000000, 3, None, 3, 0, None)   # GENERIC_READ, share read / write
    if h in (None, W.HANDLE(-1).value): say(f"open failed {C.get_last_error()}"); return
    at = HIDD_ATTRIBUTES(); at.Size = C.sizeof(at); hid.HidD_GetAttributes(W.HANDLE(h), C.byref(at))
    sbuf = C.create_unicode_buffer(256); hid.HidD_GetProductString(W.HANDLE(h), sbuf, 512)
    say(f"VID 0x{at.VendorID:04x} PID 0x{at.ProductID:04x} version 0x{at.Version:04x} \"{sbuf.value}\"")
    pp = C.c_void_p(); hid.HidD_GetPreparsedData(W.HANDLE(h), C.byref(pp))
    caps = HIDP_CAPS(); hid.HidP_GetCaps(pp, C.byref(caps))
    say(f"top-level usage page 0x{caps.UsagePage:02x} usage 0x{caps.Usage:02x}, input report {caps.InputReportByteLength} bytes")
    n = W.USHORT(caps.NumberInputValueCaps); vc = (HIDP_VALUE_CAPS * max(1, n.value))(); hid.HidP_GetValueCaps(0, vc, C.byref(n), pp)
    nb = W.USHORT(caps.NumberInputButtonCaps); bc = (HIDP_BUTTON_CAPS * max(1, nb.value))(); hid.HidP_GetButtonCaps(0, bc, C.byref(nb), pp)
    vals = []
    say("input values (descriptor order as Windows lists them):")
    for i in range(n.value):
        v = vc[i]
        for u in range(v.u.Range.UsageMin, v.u.Range.UsageMax + 1) if v.IsRange else [v.u.Range.UsageMin]:
            nm, un = name(v.UsagePage, u)
            say(f"  {nm:<10} page 0x{v.UsagePage:02x} usage 0x{u:02x}  logical {v.LogicalMin}..{v.LogicalMax}  {v.BitSize} bits  report {v.ReportID}  Unity: {un}")
            vals.append((nm, v.UsagePage, u, v.ReportID, v.LinkCollection))
    for i in range(nb.value):
        b = bc[i]; lo, hi = (b.u.Range.UsageMin, b.u.Range.UsageMax) if b.IsRange else (b.u.Range.UsageMin, b.u.Range.UsageMin)
        say(f"  buttons page 0x{b.UsagePage:02x} usage {lo}..{hi}  report {b.ReportID}")
    bpage = bc[0].UsagePage if nb.value else 9
    rep = C.create_string_buffer(caps.InputReportByteLength); got = W.DWORD()
    last = {}; t0 = time.time(); mins = {}; maxs = {}
    say(f"logging {a.seconds:.0f} s: move the throttle lever all the way forward and back")
    import threading, queue
    q = queue.Queue()
    def reader():
        while True:
            b = C.create_string_buffer(caps.InputReportByteLength); g = W.DWORD()
            if not k32.ReadFile(W.HANDLE(h), b, caps.InputReportByteLength, C.byref(g), None): q.put(None); return
            q.put(b)
    threading.Thread(target=reader, daemon=True).start()
    while time.time() - t0 < a.seconds:
        try: rep = q.get(timeout=.25)
        except queue.Empty: continue
        if rep is None: say("read failed"); break
        now = {}
        for nm, page, u, rid, lc in vals:
            val = W.ULONG(); hid.HidP_GetUsageValue(0, page, lc, u, C.byref(val), pp, rep, caps.InputReportByteLength); now[nm] = val.value
            mins[nm] = min(mins.get(nm, val.value), val.value); maxs[nm] = max(maxs.get(nm, val.value), val.value)
        cnt = W.ULONG(64); ub = (W.USHORT * 64)(); hid.HidP_GetUsages(0, bpage, 0, ub, C.byref(cnt), pp, rep, caps.InputReportByteLength)
        now["buttons"] = tuple(sorted(ub[i] for i in range(cnt.value)))
        if now != last:
            say(f"{time.time() - t0:7.3f} " + "  ".join(f"{k} {v}" for k, v in now.items() if k != "buttons") + f"  buttons {list(now['buttons'])}")
            last = now
    say("range seen: " + "  ".join(f"{k} {mins[k]}..{maxs[k]}" for k in mins))
    if out: out.close()
    os._exit(0)   # the reader thread may sit in ReadFile
    if a.out: open(a.out, "w", encoding="utf-8").write("\n".join(lines) + "\n")

if __name__ == "__main__": main()
