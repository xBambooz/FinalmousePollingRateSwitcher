using System.Runtime.InteropServices;
using HidSharp;

namespace Finalmouse.Shared;

/// <summary>
/// Communicates with Finalmouse ULX mice over HID to change polling rate.
/// </summary>
public class FinalmouseHid : IDisposable
{
    private const int VendorId = 0x361D;
    private const int ProductId = 0x0100;
    private const uint UsagePage = 0xFF00;
    private const uint Usage = 0x0001;
    private const byte ReportId = 0x04;
    private const int ReportSize = 63;

    private HidStream? _stream;
    private readonly object _lock = new();

    public bool IsConnected => _stream != null;

    public bool Open()
    {
        lock (_lock)
        {
            Close();
            try
            {
                var devices = DeviceList.Local.GetHidDevices(VendorId, ProductId);
                HidDevice? target = null;

                foreach (var dev in devices)
                {
                    try
                    {
                        var reportDesc = dev.GetReportDescriptor();
                        foreach (var item in reportDesc.DeviceItems)
                        {
                            foreach (var u in item.Usages.GetAllValues())
                            {
                                uint page = (uint)(u >> 16);
                                uint usage = (uint)(u & 0xFFFF);
                                if (page == UsagePage && usage == Usage)
                                {
                                    target = dev;
                                    break;
                                }
                            }
                            if (target != null) break;
                        }
                    }
                    catch { continue; }
                    if (target != null) break;
                }

                if (target == null)
                    return false;

                _stream = target.Open();
                return true;
            }
            catch
            {
                _stream = null;
                return false;
            }
        }
    }

    public bool SetPollingRate(int hz)
    {
        lock (_lock)
        {
            if (_stream == null && !Open())
                return false;

            try
            {
                var data = new byte[ReportSize + 1]; // +1 for report ID prefix
                data[0] = ReportId;  // Report ID
                data[1] = 0x04;      // Command marker
                data[2] = 0x91;      // Polling rate command
                data[3] = 0x02;      // Sub-command
                data[4] = (byte)(hz & 0xFF);         // Rate low byte
                data[5] = (byte)((hz >> 8) & 0xFF);  // Rate high byte

                _stream!.Write(data);
                return true;
            }
            catch
            {
                Close();
                return false;
            }
        }
    }

    public void Close()
    {
        lock (_lock)
        {
            try { _stream?.Close(); } catch { }
            _stream = null;
        }
    }

    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }
}
