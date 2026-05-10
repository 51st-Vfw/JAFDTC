namespace JAFDTC.File.MIZ.DTC
{
    [System.Flags]
    public enum Sections
    {
        None = 0,
        COMMS = 1 << 0,
        STPS = 1 << 1,
        CMDS = 1 << 2,
        ELINT = 1 << 3
    }
}
