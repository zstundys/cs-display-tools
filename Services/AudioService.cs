using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;
using PropertyKey = NAudio.CoreAudioApi.PropertyKey;

namespace DisplayRefreshRate.Services;

public class AudioService : IMMNotificationClient, IDisposable
{
    private readonly SettingsService _settings;
    private MMDeviceEnumerator? _deviceEnumerator;
    private bool _isSecondaryDisplay;
    private bool _initialized;
    private volatile int _suppressProgrammaticNotification;
    private string _lastProgrammaticSetName = string.Empty;

    // Undocumented IPolicyConfig interface for setting default audio endpoint
    [ComImport]
    [Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        void GetMixFormat(string deviceId, out IntPtr format);
        void GetDeviceFormat(string deviceId, int flow, out IntPtr format);
        void ResetDeviceFormat(string deviceId);
        void SetDeviceFormat(string deviceId, IntPtr endpointFormat, IntPtr mixFormat);
        void GetProcessingPeriod(string deviceId, int flow, out long defaultPeriod);
        void SetProcessingPeriod(string deviceId, ref long period);
        void GetShareMode(string deviceId, out IntPtr shareMode);
        void SetShareMode(string deviceId, IntPtr shareMode);
        void GetPropertyValue(string deviceId, ref PolicyPropertyKey key, out PolicyPropVariant value);
        void SetPropertyValue(string deviceId, ref PolicyPropertyKey key, ref PolicyPropVariant value);
        [PreserveSig]
        int SetDefaultEndpoint(string deviceId, Role role);
        void SetEndpointVisibility(string deviceId, int isVisible);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PolicyPropertyKey
    {
        public Guid fmtid;
        public int pid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PolicyPropVariant
    {
        public ushort vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;
        public IntPtr p1;
        public IntPtr p2;
    }

    private static readonly Guid CLSID_PolicyConfigClient = new("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9");

    public AudioService(SettingsService settings)
    {
        _settings = settings;
    }

    public void Initialize()
    {
        if (_initialized) return;

        try
        {
            _deviceEnumerator = new MMDeviceEnumerator();
            _deviceEnumerator.RegisterEndpointNotificationCallback(this);
            _initialized = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize audio: {ex.Message}");
        }
    }

    public void Shutdown()
    {
        if (_deviceEnumerator != null)
        {
            try
            {
                _deviceEnumerator.UnregisterEndpointNotificationCallback(this);
            }
            catch { }
            _deviceEnumerator.Dispose();
            _deviceEnumerator = null;
        }
        _initialized = false;
    }

    public void SetCurrentDisplayMode(bool isSecondary)
    {
        _isSecondaryDisplay = isSecondary;
    }

    public bool SetDefaultAudioForMode(bool switchingToExternal)
    {
        string target = switchingToExternal ? _settings.SecondaryDevice : _settings.PrimaryDevice;

        if (!string.IsNullOrEmpty(target))
        {
            // Wait longer for external display (HDMI audio takes time to appear)
            int timeoutMs = switchingToExternal ? 8000 : 3000;
            
            if (WaitForAudioDeviceActive(target, timeoutMs, 250))
            {
                // Retry up to 3 times
                for (int i = 0; i < 3; i++)
                {
                    if (SetDefaultAudioDeviceByName(target))
                        return true;
                    Thread.Sleep(2000);
                }
            }
        }

        // Learning mode: save whatever device is now default
        Thread.Sleep(1500);
        string? currentName = GetDefaultRenderDeviceName();
        if (!string.IsNullOrEmpty(currentName))
        {
            if (switchingToExternal)
                _settings.SecondaryDevice = currentName;
            else
                _settings.PrimaryDevice = currentName;
            _settings.Save();
        }

        return true;
    }

    public bool SetDefaultAudioDeviceByName(string friendlyName)
    {
        if (string.IsNullOrEmpty(friendlyName)) return false;

        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active | DeviceState.Disabled);

            string? deviceId = null;

            // First pass: exact match
            foreach (var device in devices)
            {
                if (string.Equals(device.FriendlyName, friendlyName, StringComparison.OrdinalIgnoreCase))
                {
                    deviceId = device.ID;
                    break;
                }
            }

            // Second pass: substring match
            if (deviceId == null)
            {
                foreach (var device in devices)
                {
                    if (device.FriendlyName.Contains(friendlyName, StringComparison.OrdinalIgnoreCase))
                    {
                        deviceId = device.ID;
                        break;
                    }
                }
            }

            if (deviceId != null)
            {
                _lastProgrammaticSetName = friendlyName;
                Interlocked.Exchange(ref _suppressProgrammaticNotification, 3);

                // Use undocumented IPolicyConfig to set default endpoint
                var policyConfigType = Type.GetTypeFromCLSID(CLSID_PolicyConfigClient);
                if (policyConfigType != null)
                {
                    var policyConfig = (IPolicyConfig?)Activator.CreateInstance(policyConfigType);
                    if (policyConfig != null)
                    {
                        policyConfig.SetDefaultEndpoint(deviceId, Role.Console);
                        policyConfig.SetDefaultEndpoint(deviceId, Role.Multimedia);
                        policyConfig.SetDefaultEndpoint(deviceId, Role.Communications);
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SetDefaultAudioDeviceByName failed: {ex.Message}");
        }

        return false;
    }

    private bool IsAudioDeviceActiveByName(string friendlyName)
    {
        if (string.IsNullOrEmpty(friendlyName)) return false;

        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            foreach (var device in devices)
            {
                if (string.Equals(device.FriendlyName, friendlyName, StringComparison.OrdinalIgnoreCase) ||
                    device.FriendlyName.Contains(friendlyName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch { }

        return false;
    }

    private bool WaitForAudioDeviceActive(string friendlyName, int timeoutMs, int pollMs)
    {
        int waited = 0;
        while (waited <= timeoutMs)
        {
            if (IsAudioDeviceActiveByName(friendlyName))
                return true;
            Thread.Sleep(pollMs);
            waited += pollMs;
        }
        return false;
    }

    private string? GetDefaultRenderDeviceName()
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            return device?.FriendlyName;
        }
        catch
        {
            return null;
        }
    }

    #region IMMNotificationClient Implementation

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (flow != DataFlow.Render || string.IsNullOrEmpty(defaultDeviceId)) return;

        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDevice(defaultDeviceId);
            string name = device?.FriendlyName ?? string.Empty;

            if (string.IsNullOrEmpty(name)) return;

            // Suppress notification if this was our programmatic change
            if (_suppressProgrammaticNotification > 0)
            {
                if (string.Equals(name, _lastProgrammaticSetName, StringComparison.OrdinalIgnoreCase))
                {
                    Interlocked.Decrement(ref _suppressProgrammaticNotification);
                    return;
                }
            }

            // User changed the default device manually - learn it
            if (_isSecondaryDisplay)
                _settings.SecondaryDevice = name;
            else
                _settings.PrimaryDevice = name;

            _settings.Save();
        }
        catch { }
    }

    public void OnDeviceAdded(string pwstrDeviceId) { }
    public void OnDeviceRemoved(string deviceId) { }
    public void OnDeviceStateChanged(string deviceId, DeviceState newState) { }
    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }

    #endregion

    public void Dispose()
    {
        Shutdown();
    }
}

