using System;

namespace BluePointLilac.Methods
{
    // 判断Windows系统版本
    // https://docs.microsoft.com/windows/release-health/release-information
    public static class WinOsVersion
    {
        public static readonly Version Current = Environment.OSVersion.Version;
        public static readonly Version Win11 = new(10, 0, 22000);
        public static readonly Version Win10 = new(10, 0);
        public static readonly Version Win8_1 = new(6, 3);
        public static readonly Version Win8 = new(6, 2);
        public static readonly Version Win7 = new(6, 1);
        public static readonly Version Vista = new(6, 0);
        public static readonly Version XP = new(5, 1);

        public static readonly Version Win10_1507 = new(10, 0, 10240);
        public static readonly Version Win10_1511 = new(10, 0, 10586);
        public static readonly Version Win10_1607 = new(10, 0, 14393);
        public static readonly Version Win10_1703 = new(10, 0, 15063);
        public static readonly Version Win10_1709 = new(10, 0, 16299);
        public static readonly Version Win10_1803 = new(10, 0, 17134);
        public static readonly Version Win10_1809 = new(10, 0, 17763);
        public static readonly Version Win10_1903 = new(10, 0, 18362);
        public static readonly Version Win10_1909 = new(10, 0, 18363);
        public static readonly Version Win10_2004 = new(10, 0, 19041);
        public static readonly Version Win10_20H2 = new(10, 0, 19042);
    }
}