using System.Reflection;

namespace TentoUI;

public static class LucideIcons
{
    private static readonly Dictionary<string, string> SvgPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        [IconNames.Check] = "<polyline points=\"20 6 9 17 4 12\"></polyline>",
        [IconNames.CheckCircle] = "<path d=\"M22 11.08V12a10 10 0 1 1-5.93-9.14\"></path><polyline points=\"22 4 12 14.01 9 11.01\"></polyline>",
        [IconNames.CheckCircle2] = "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><path d=\"m9 12 2 2 4-4\"></path>",
        [IconNames.AlertCircle] = "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><line x1=\"12\" y1=\"8\" x2=\"12\" y2=\"12\"></line><line x1=\"12\" y1=\"16\" x2=\"12.01\" y2=\"16\"></line>",
        [IconNames.AlertTriangle] = "<path d=\"m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z\"></path><line x1=\"12\" y1=\"9\" x2=\"12\" y2=\"13\"></line><line x1=\"12\" y1=\"17\" x2=\"12.01\" y2=\"17\"></line>",
        [IconNames.Info] = "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><line x1=\"12\" y1=\"16\" x2=\"12\" y2=\"12\"></line><line x1=\"12\" y1=\"8\" x2=\"12.01\" y2=\"8\"></line>",
        [IconNames.X] = "<line x1=\"18\" y1=\"6\" x2=\"6\" y2=\"18\"></line><line x1=\"6\" y1=\"6\" x2=\"18\" y2=\"18\"></line>",
        [IconNames.Close] = "<line x1=\"18\" y1=\"6\" x2=\"6\" y2=\"18\"></line><line x1=\"6\" y1=\"6\" x2=\"18\" y2=\"18\"></line>",
        [IconNames.XCircle] = "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><line x1=\"15\" y1=\"9\" x2=\"9\" y2=\"15\"></line><line x1=\"9\" y1=\"9\" x2=\"15\" y2=\"15\"></line>",
        [IconNames.Loader] = "<line x1=\"12\" y1=\"2\" x2=\"12\" y2=\"6\"></line><line x1=\"12\" y1=\"18\" x2=\"12\" y2=\"22\"></line><line x1=\"4.93\" y1=\"4.93\" x2=\"7.76\" y2=\"7.76\"></line><line x1=\"16.24\" y1=\"16.24\" x2=\"19.07\" y2=\"19.07\"></line><line x1=\"2\" y1=\"12\" x2=\"6\" y2=\"12\"></line><line x1=\"18\" y1=\"12\" x2=\"22\" y2=\"12\"></line><line x1=\"4.93\" y1=\"19.07\" x2=\"7.76\" y2=\"16.24\"></line><line x1=\"16.24\" y1=\"7.76\" x2=\"19.07\" y2=\"4.93\"></line>",
        [IconNames.Loader2] = "<path d=\"M21 12a9 9 0 1 1-6.219-8.56\"></path>",
        [IconNames.ChevronDown] = "<polyline points=\"6 9 12 15 18 9\"></polyline>",
        [IconNames.ChevronUp] = "<polyline points=\"18 15 12 9 6 15\"></polyline>",
        [IconNames.ChevronLeft] = "<polyline points=\"15 18 9 12 15 6\"></polyline>",
        [IconNames.ChevronRight] = "<polyline points=\"9 18 15 12 9 6\"></polyline>",
        [IconNames.ArrowLeft] = "<line x1=\"19\" y1=\"12\" x2=\"5\" y2=\"12\"></line><polyline points=\"12 19 5 12 12 5\"></polyline>",
        [IconNames.ArrowRight] = "<line x1=\"5\" y1=\"12\" x2=\"19\" y2=\"12\"></line><polyline points=\"12 5 19 12 12 19\"></polyline>",
        [IconNames.ArrowUp] = "<line x1=\"12\" y1=\"19\" x2=\"12\" y2=\"5\"></line><polyline points=\"5 12 12 5 19 12\"></polyline>",
        [IconNames.ArrowDown] = "<line x1=\"12\" y1=\"5\" x2=\"12\" y2=\"19\"></line><polyline points=\"19 12 12 19 5 12\"></polyline>",
        [IconNames.Plus] = "<line x1=\"12\" y1=\"5\" x2=\"12\" y2=\"19\"></line><line x1=\"5\" y1=\"12\" x2=\"19\" y2=\"12\"></line>",
        [IconNames.Minus] = "<line x1=\"5\" y1=\"12\" x2=\"19\" y2=\"12\"></line>",
        [IconNames.Search] = "<circle cx=\"11\" cy=\"11\" r=\"8\"></circle><line x1=\"21\" y1=\"21\" x2=\"16.65\" y2=\"16.65\"></line>",
        [IconNames.Copy] = "<rect x=\"9\" y=\"9\" width=\"13\" height=\"13\" rx=\"2\" ry=\"2\"></rect><path d=\"M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1\"></path>",
        [IconNames.Edit] = "<path d=\"M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7\"></path><path d=\"M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z\"></path>",
        [IconNames.Edit2] = "<path d=\"M17 3a2.828 2.828 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5L17 3z\"></path>",
        [IconNames.Trash] = "<polyline points=\"3 6 5 6 21 6\"></polyline><path d=\"M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2\"></path>",
        [IconNames.Trash2] = "<polyline points=\"3 6 5 6 21 6\"></polyline><path d=\"M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2\"></path><line x1=\"10\" y1=\"11\" x2=\"10\" y2=\"17\"></line><line x1=\"14\" y1=\"11\" x2=\"14\" y2=\"17\"></line>",
        [IconNames.ExternalLink] = "<path d=\"M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6\"></path><polyline points=\"15 3 21 3 21 9\"></polyline><line x1=\"10\" y1=\"14\" x2=\"21\" y2=\"3\"></line>",
        [IconNames.Link] = "<path d=\"M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71\"></path><path d=\"M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71\"></path>",
        [IconNames.Filter] = "<polygon points=\"22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3\"></polygon>",
        [IconNames.User] = "<path d=\"M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2\"></path><circle cx=\"12\" cy=\"7\" r=\"4\"></circle>",
        [IconNames.Calendar] = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\" ry=\"2\"></rect><line x1=\"16\" y1=\"2\" x2=\"16\" y2=\"6\"></line><line x1=\"8\" y1=\"2\" x2=\"8\" y2=\"6\"></line><line x1=\"3\" y1=\"10\" x2=\"21\" y2=\"10\"></line>",
        [IconNames.Clock] = "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><polyline points=\"12 6 12 12 16 14\"></polyline>",
        [IconNames.Sun] = "<circle cx=\"12\" cy=\"12\" r=\"5\"></circle><line x1=\"12\" y1=\"1\" x2=\"12\" y2=\"3\"></line><line x1=\"12\" y1=\"21\" x2=\"12\" y2=\"23\"></line><line x1=\"4.22\" y1=\"4.22\" x2=\"5.64\" y2=\"5.64\"></line><line x1=\"18.36\" y1=\"18.36\" x2=\"19.78\" y2=\"19.78\"></line><line x1=\"1\" y1=\"12\" x2=\"3\" y2=\"12\"></line><line x1=\"21\" y1=\"12\" x2=\"23\" y2=\"12\"></line><line x1=\"4.22\" y1=\"19.07\" x2=\"5.64\" y2=\"17.64\"></line><line x1=\"18.36\" y1=\"5.64\" x2=\"19.78\" y2=\"4.22\"></line>",
        [IconNames.Moon] = "<path d=\"M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z\"></path>",
        [IconNames.Menu] = "<line x1=\"3\" y1=\"12\" x2=\"21\" y2=\"12\"></line><line x1=\"3\" y1=\"6\" x2=\"21\" y2=\"6\"></line><line x1=\"3\" y1=\"18\" x2=\"21\" y2=\"18\"></line>",
        [IconNames.Eye] = "<path d=\"M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z\"></path><circle cx=\"12\" cy=\"12\" r=\"3\"></circle>",
        [IconNames.EyeOff] = "<path d=\"M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24\"></path><line x1=\"1\" y1=\"1\" x2=\"23\" y2=\"23\"></line>",
        [IconNames.RefreshCw] = "<polyline points=\"23 4 23 10 17 10\"></polyline><polyline points=\"1 20 1 14 7 14\"></polyline><path d=\"M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15\"></path>",
        [IconNames.HelpCircle] = "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><path d=\"M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3\"></path><line x1=\"12\" y1=\"17\" x2=\"12.01\" y2=\"17\"></line>",
        [IconNames.Star] = "<polygon points=\"12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2\"></polygon>",
        [IconNames.Sparkles] = "<path d=\"m12 3-1.912 5.813a2 2 0 0 1-1.275 1.275L3 12l5.813 1.912a2 2 0 0 1 1.275 1.275L12 21l1.912-5.813a2 2 0 0 1 1.275-1.275L21 12l-5.813-1.912a2 2 0 0 1-1.275-1.275L12 3Z\"></path><path d=\"M5 3v4\"></path><path d=\"M19 17v4\"></path><path d=\"M3 5h4\"></path><path d=\"M17 19h4\"></path>",
        [IconNames.Upload] = "<path d=\"M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4\"></path><polyline points=\"17 8 12 3 7 8\"></polyline><line x1=\"12\" y1=\"3\" x2=\"12\" y2=\"15\"></line>",
        [IconNames.Download] = "<path d=\"M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4\"></path><polyline points=\"7 10 12 15 17 10\"></polyline><line x1=\"12\" y1=\"15\" x2=\"12\" y2=\"3\"></line>",
        [IconNames.Inbox] = "<polyline points=\"22 12 16 12 14 15 10 15 8 12 2 12\"></polyline><path d=\"M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z\"></path>",
        [IconNames.MoreHorizontal] = "<circle cx=\"12\" cy=\"12\" r=\"1\"></circle><circle cx=\"19\" cy=\"12\" r=\"1\"></circle><circle cx=\"5\" cy=\"12\" r=\"1\"></circle>",
        [IconNames.MoreVertical] = "<circle cx=\"12\" cy=\"12\" r=\"1\"></circle><circle cx=\"12\" cy=\"5\" r=\"1\"></circle><circle cx=\"12\" cy=\"19\" r=\"1\"></circle>",
        [IconNames.Settings] = "<circle cx=\"12\" cy=\"12\" r=\"3\"></circle><path d=\"M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z\"></path>",
        [IconNames.Bell] = "<path d=\"M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9\"></path><path d=\"M13.73 21a2 2 0 0 1-3.46 0\"></path>",
        [IconNames.Home] = "<path d=\"m3 9 9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z\"></path><polyline points=\"9 22 9 12 15 12 15 22\"></polyline>",
        [IconNames.File] = "<path d=\"M14.5 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7.5L14.5 2z\"></path><polyline points=\"14 2 14 8 20 8\"></polyline>",
        [IconNames.Folder] = "<path d=\"M4 20h16a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.93a2 2 0 0 1-1.66-.9l-.82-1.2A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13c0 1.1.9 2 2 2Z\"></path>",
        [IconNames.Play] = "<polygon points=\"5 3 19 12 5 21 5 3\"></polygon>",
        [IconNames.Pause] = "<rect x=\"6\" y=\"4\" width=\"4\" height=\"16\"></rect><rect x=\"14\" y=\"4\" width=\"4\" height=\"16\"></rect>",
        [IconNames.Music] = "<path d=\"M9 18V5l12-2v13\"></path><circle cx=\"6\" cy=\"18\" r=\"3\"></circle><circle cx=\"18\" cy=\"16\" r=\"3\"></circle>",
        [IconNames.RotateCcw] = "<path d=\"M1 4v6h6\"></path><path d=\"M3.51 15a9 9 0 1 0 2.13-9.36L1 10\"></path>",
        [IconNames.RotateCw] = "<path d=\"M23 4v6h-6\"></path><path d=\"M20.49 15a9 9 0 1 1-2.12-9.36L23 10\"></path>",
        [IconNames.VolumeX] = "<polygon points=\"11 5 6 9 2 9 2 15 6 15 11 19 11 5\"></polygon><line x1=\"23\" y1=\"9\" x2=\"17\" y2=\"15\"></line><line x1=\"17\" y1=\"9\" x2=\"23\" y2=\"15\"></line>",
        [IconNames.Volume1] = "<polygon points=\"11 5 6 9 2 9 2 15 6 15 11 19 11 5\"></polygon><path d=\"M15.54 8.46a5 5 0 0 1 0 7.07\"></path>",
        [IconNames.Volume2] = "<polygon points=\"11 5 6 9 2 9 2 15 6 15 11 19 11 5\"></polygon><path d=\"M19.07 4.93a10 10 0 0 1 0 14.14M15.54 8.46a5 5 0 0 1 0 7.07\"></path>",
        [IconNames.Square] = "<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"2\" ry=\"2\"></rect>",
        [IconNames.Building] = "<rect x=\"4\" y=\"2\" width=\"16\" height=\"20\" rx=\"2\" ry=\"2\"></rect><path d=\"M9 22v-4h6v4\"></path><path d=\"M8 6h.01\"></path><path d=\"M16 6h.01\"></path><path d=\"M8 10h.01\"></path><path d=\"M16 10h.01\"></path><path d=\"M8 14h.01\"></path><path d=\"M16 14h.01\"></path>",
        [IconNames.Lock] = "<rect x=\"3\" y=\"11\" width=\"18\" height=\"11\" rx=\"2\" ry=\"2\"></rect><path d=\"M7 11V7a5 5 0 0 1 10 0v4\"></path>",
        [IconNames.CreditCard] = "<rect x=\"1\" y=\"4\" width=\"22\" height=\"16\" rx=\"2\" ry=\"2\"></rect><line x1=\"1\" y1=\"10\" x2=\"23\" y2=\"10\"></line>",

        // Device & Hardware
        [IconNames.Smartphone] = "<rect width=\"14\" height=\"20\" x=\"5\" y=\"2\" rx=\"2\" ry=\"2\"></rect><path d=\"M12 18h.01\"></path>",
        [IconNames.Tablet] = "<rect width=\"16\" height=\"20\" x=\"4\" y=\"2\" rx=\"2\" ry=\"2\"></rect><line x1=\"12\" x2=\"12.01\" y1=\"18\" y2=\"18\"></line>",
        [IconNames.Laptop] = "<path d=\"M20 16V7a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v9m16 0H4m16 0 1.28 2.55a1 1 0 0 1-.9 1.45H3.62a1 1 0 0 1-.9-1.45L4 16\"></path>",
        [IconNames.Monitor] = "<rect width=\"20\" height=\"14\" x=\"2\" y=\"3\" rx=\"2\"></rect><line x1=\"8\" x2=\"16\" y1=\"21\" y2=\"21\"></line><line x1=\"12\" x2=\"12\" y1=\"17\" y2=\"21\"></line>",
        [IconNames.Tv] = "<rect width=\"20\" height=\"15\" x=\"2\" y=\"7\" rx=\"2\"></rect><polyline points=\"17 2 12 7 7 2\"></polyline>",
        [IconNames.Printer] = "<polyline points=\"6 9 6 2 18 2 18 9\"></polyline><path d=\"M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2\"></path><rect width=\"12\" height=\"8\" x=\"6\" y=\"14\"></rect>",
        [IconNames.HardDrive] = "<line x1=\"22\" x2=\"2\" y1=\"12\" y2=\"12\"></line><path d=\"M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z\"></path><line x1=\"6\" x2=\"6.01\" y1=\"16\" y2=\"16\"></line><line x1=\"10\" x2=\"10.01\" y1=\"16\" y2=\"16\"></line>",
        [IconNames.Cpu] = "<rect width=\"16\" height=\"16\" x=\"4\" y=\"4\" rx=\"2\"></rect><rect width=\"6\" height=\"6\" x=\"9\" y=\"9\"></rect><path d=\"M15 2v2M15 20v2M2 15h2M2 9h2M20 15h2M20 9h2M9 2v2M9 20v2\"></path>",
        [IconNames.Server] = "<rect width=\"20\" height=\"8\" x=\"2\" y=\"2\" rx=\"2\" ry=\"2\"></rect><rect width=\"20\" height=\"8\" x=\"2\" y=\"14\" rx=\"2\" ry=\"2\"></rect><line x1=\"6\" x2=\"6.01\" y1=\"6\" y2=\"6\"></line><line x1=\"6\" x2=\"6.01\" y1=\"18\" y2=\"18\"></line>",
        [IconNames.Database] = "<ellipse cx=\"12\" cy=\"5\" rx=\"9\" ry=\"3\"></ellipse><path d=\"M3 5V19A9 3 0 0 0 21 19V5\"></path><path d=\"M3 12A9 3 0 0 0 21 12\"></path>",
        [IconNames.Wifi] = "<path d=\"M5 13a10 10 0 0 1 14 0\"></path><path d=\"M8.5 16.5a5 5 0 0 1 7 0\"></path><path d=\"M2 8.82a15 15 0 0 1 20 0\"></path><line x1=\"12\" x2=\"12.01\" y1=\"20\" y2=\"20\"></line>",
        [IconNames.WifiOff] = "<line x1=\"2\" x2=\"22\" y1=\"2\" y2=\"22\"></line><path d=\"M8.5 16.5a5 5 0 0 1 7 0\"></path><path d=\"M2 8.82a15 15 0 0 1 4.17-2.65\"></path><path d=\"M10.66 5c4.01-.36 8.14.9 11.34 3.82\"></path><path d=\"M16.85 11.25a10 10 0 0 1 2.22 1.78\"></path><path d=\"M5 13a10 10 0 0 1 5.24-2.76\"></path><line x1=\"12\" x2=\"12.01\" y1=\"20\" y2=\"20\"></line>",
        [IconNames.Bluetooth] = "<path d=\"m7 7 10 10-5 5V2l5 5L7 17\"></path>",
        [IconNames.Battery] = "<rect width=\"16\" height=\"10\" x=\"2\" y=\"7\" rx=\"2\" ry=\"2\"></rect><line x1=\"22\" x2=\"22\" y1=\"11\" y2=\"13\"></line>",
        [IconNames.BatteryCharging] = "<path d=\"M15 7h1a2 2 0 0 1 2 2v6a2 2 0 0 1-2 2h-2\"></path><path d=\"M6 7H4a2 2 0 0 0-2 2v6a2 2 0 0 0 2 2h1\"></path><line x1=\"22\" x2=\"22\" y1=\"11\" y2=\"13\"></line><polygon points=\"11 6 7 12 13 12 9 18 17 11 11 11 11 6\"></polygon>",

        // Actions & Productivity
        [IconNames.Save] = "<path d=\"M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z\"></path><polyline points=\"17 21 17 13 7 13 7 21\"></polyline><polyline points=\"7 3 7 8 15 8\"></polyline>",
        [IconNames.Bookmark] = "<path d=\"m19 21-7-4-7 4V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v16z\"></path>",
        [IconNames.Pin] = "<line x1=\"12\" x2=\"12\" y1=\"17\" y2=\"22\"></line><path d=\"M5 17h14v-1.76a2 2 0 0 0-1.11-1.79l-1.78-.9A2 2 0 0 1 15 10.76V6h1a2 2 0 0 0 0-4H8a2 2 0 0 0 0 4h1v4.76a2 2 0 0 1-1.11 1.79l-1.78.9A2 2 0 0 0 5 15.24Z\"></path>",
        [IconNames.Send] = "<line x1=\"22\" x2=\"11\" y1=\"2\" y2=\"13\"></line><polygon points=\"22 2 15 22 11 13 2 9 22 2\"></polygon>",
        [IconNames.Share] = "<path d=\"M4 12v8a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-8\"></path><polyline points=\"16 6 12 2 8 6\"></polyline><line x1=\"12\" x2=\"12\" y1=\"2\" y2=\"15\"></line>",
        [IconNames.Share2] = "<circle cx=\"18\" cy=\"5\" r=\"3\"></circle><circle cx=\"6\" cy=\"12\" r=\"3\"></circle><circle cx=\"18\" cy=\"19\" r=\"3\"></circle><line x1=\"8.59\" x2=\"15.42\" y1=\"13.51\" y2=\"17.49\"></line><line x1=\"15.41\" x2=\"8.59\" y1=\"6.51\" y2=\"10.49\"></line>",
        [IconNames.Terminal] = "<polyline points=\"4 17 10 11 4 5\"></polyline><line x1=\"12\" x2=\"20\" y1=\"19\" y2=\"19\"></line>",
        [IconNames.Code] = "<polyline points=\"16 18 22 12 16 6\"></polyline><polyline points=\"8 6 2 12 8 18\"></polyline>",
        [IconNames.Code2] = "<path d=\"m18 16 4-4-4-4\"></path><path d=\"m6 8-4 4 4 4\"></path><path d=\"m14.5 4-5 16\"></path>",
        [IconNames.Layers] = "<polygon points=\"12 2 2 7 12 12 22 7 12 2\"></polygon><polyline points=\"2 17 12 22 22 17\"></polyline><polyline points=\"2 12 12 17 22 12\"></polyline>",
        [IconNames.Sliders] = "<line x1=\"4\" x2=\"4\" y1=\"21\" y2=\"14\"></line><line x1=\"4\" x2=\"4\" y1=\"10\" y2=\"3\"></line><line x1=\"12\" x2=\"12\" y1=\"21\" y2=\"12\"></line><line x1=\"12\" x2=\"12\" y1=\"8\" y2=\"3\"></line><line x1=\"20\" x2=\"20\" y1=\"21\" y2=\"16\"></line><line x1=\"20\" x2=\"20\" y1=\"12\" y2=\"3\"></line><line x1=\"1\" x2=\"7\" y1=\"14\" y2=\"14\"></line><line x1=\"9\" x2=\"15\" y1=\"8\" y2=\"8\"></line><line x1=\"17\" x2=\"23\" y1=\"16\" y2=\"16\"></line>",
        [IconNames.Compass] = "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><polygon points=\"16.24 7.76 14.12 14.12 7.76 16.24 9.88 9.88 16.24 7.76\"></polygon>",
        [IconNames.MapPin] = "<path d=\"M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z\"></path><circle cx=\"12\" cy=\"10\" r=\"3\"></circle>",
        [IconNames.Navigation] = "<polygon points=\"3 11 22 2 13 21 11 13 3 11\"></polygon>",
        [IconNames.Crosshair] = "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><line x1=\"22\" x2=\"18\" y1=\"12\" y2=\"12\"></line><line x1=\"6\" x2=\"2\" y1=\"12\" y2=\"12\"></line><line x1=\"12\" x2=\"12\" y1=\"6\" y2=\"2\"></line><line x1=\"12\" x2=\"12\" y1=\"22\" y2=\"18\"></line>",
        [IconNames.Globe] = "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><line x1=\"2\" x2=\"22\" y1=\"12\" y2=\"12\"></line><path d=\"M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z\"></path>",
        [IconNames.Maximize] = "<path d=\"M8 3H5a2 2 0 0 0-2 2v3m18 0V5a2 2 0 0 0-2-2h-3m0 18h3a2 2 0 0 0 2-2v-3M3 16v3a2 2 0 0 0 2 2h3\"></path>",
        [IconNames.Minimize] = "<path d=\"M4 14h6v6m10-10h-6V4m0 6 7-7M3 21l7-7\"></path>",
        [IconNames.Maximize2] = "<polyline points=\"15 3 21 3 21 9\"></polyline><polyline points=\"9 21 3 21 3 15\"></polyline><line x1=\"21\" x2=\"14\" y1=\"3\" y2=\"10\"></line><line x1=\"3\" x2=\"10\" y1=\"21\" y2=\"14\"></line>",
        [IconNames.Minimize2] = "<polyline points=\"4 14 10 14 10 20\"></polyline><polyline points=\"20 10 14 10 14 4\"></polyline><line x1=\"14\" x2=\"21\" y1=\"10\" y2=\"3\"></line><line x1=\"3\" x2=\"10\" y1=\"21\" y2=\"14\"></line>",
        [IconNames.ZoomIn] = "<circle cx=\"11\" cy=\"11\" r=\"8\"></circle><line x1=\"21\" x2=\"16.65\" y1=\"21\" y2=\"16.65\"></line><line x1=\"11\" x2=\"11\" y1=\"8\" y2=\"14\"></line><line x1=\"8\" x2=\"14\" y1=\"11\" y2=\"11\"></line>",
        [IconNames.ZoomOut] = "<circle cx=\"11\" cy=\"11\" r=\"8\"></circle><line x1=\"21\" x2=\"16.65\" y1=\"21\" y2=\"16.65\"></line><line x1=\"8\" x2=\"14\" y1=\"11\" y2=\"11\"></line>",
        [IconNames.History] = "<path d=\"M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8\"></path><path d=\"M3 3v5h5\"></path><path d=\"M12 7v5l4 2\"></path>",
        [IconNames.Flag] = "<path d=\"M4 15s1-1 4-1 5 2 8 2 4-1 4-1V3s-1 1-4 1-5-2-8-2-4 1-4 1z\"></path><line x1=\"4\" x2=\"4\" y1=\"22\" y2=\"15\"></line>",
        [IconNames.Tag] = "<path d=\"M12 2H2v10l9.29 9.29c.94.94 2.48.94 3.42 0l6.58-6.58c.94-.94.94-2.48 0-3.42L12 2Z\"></path><path d=\"M7 7h.01\"></path>",
        [IconNames.Archive] = "<rect width=\"20\" height=\"5\" x=\"2\" y=\"3\" rx=\"1\"></rect><path d=\"M4 8v11a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8\"></path><path d=\"M10 12h4\"></path>",

        // Communication, Social & Media
        [IconNames.Mail] = "<rect width=\"20\" height=\"16\" x=\"2\" y=\"4\" rx=\"2\"></rect><path d=\"m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7\"></path>",
        [IconNames.MessageSquare] = "<path d=\"M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z\"></path>",
        [IconNames.MessageCircle] = "<path d=\"m3 21 1.9-5.7a8.5 8.5 0 1 1 3.8 3.8z\"></path>",
        [IconNames.Phone] = "<path d=\"M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z\"></path>",
        [IconNames.PhoneCall] = "<path d=\"M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z\"></path><path d=\"M14.05 2a9 9 0 0 1 8 7.94\"></path><path d=\"M14.05 6A5 5 0 0 1 18 10\"></path>",
        [IconNames.PhoneOff] = "<path d=\"M10.68 13.31a16 16 0 0 0 3.41 2.6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7 2 2 0 0 1 1.72 2v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.42 19.42 0 0 1-3.33-2.67m-2.67-3.34a19.79 19.79 0 0 1-3.07-8.63A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91\"></path><line x1=\"2\" x2=\"22\" y1=\"2\" y2=\"22\"></line>",
        [IconNames.Video] = "<polygon points=\"23 7 16 12 23 17 23 7\"></polygon><rect width=\"15\" height=\"14\" x=\"1\" y=\"5\" rx=\"2\" ry=\"2\"></rect>",
        [IconNames.VideoOff] = "<path d=\"m2 2 20 20\"></path><path d=\"M10.66 6H14a2 2 0 0 1 2 2v2.34l1 1L23 7v10\"></path><path d=\"M16 16a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h2l10 10Z\"></path>",
        [IconNames.Mic] = "<path d=\"M12 2a3 3 0 0 0-3 3v7a3 3 0 0 0 6 0V5a3 3 0 0 0-3-3Z\"></path><path d=\"M19 10v2a7 7 0 0 1-14 0v-2\"></path><line x1=\"12\" x2=\"12\" y1=\"19\" y2=\"22\"></line>",
        [IconNames.MicOff] = "<line x1=\"2\" x2=\"22\" y1=\"2\" y2=\"22\"></line><path d=\"M18.89 13.23A7.12 7.12 0 0 0 19 12v-2\"></path><path d=\"M5 10v2a7 7 0 0 0 12 5\"></path><path d=\"M15 9.34V5a3 3 0 0 0-5.68-1.33\"></path><path d=\"M9 9v3a3 3 0 0 0 5.12 2.12\"></path><line x1=\"12\" x2=\"12\" y1=\"19\" y2=\"22\"></line>",
        [IconNames.Camera] = "<path d=\"M14.5 4h-5L7 7H4a2 2 0 0 0-2 2v9a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2h-3l-2.5-3z\"></path><circle cx=\"12\" cy=\"13\" r=\"3\"></circle>",
        [IconNames.Image] = "<rect width=\"18\" height=\"18\" x=\"3\" y=\"3\" rx=\"2\" ry=\"2\"></rect><circle cx=\"9\" cy=\"9\" r=\"2\"></circle><path d=\"m21 15-3.086-3.086a2 2 0 0 0-2.828 0L6 21\"></path>",
        [IconNames.Radio] = "<path d=\"M4.9 19.1C1 15.2 1 8.8 4.9 4.9\"></path><path d=\"M7.8 16.2c-2.3-2.3-2.3-6.1 0-8.5\"></path><circle cx=\"12\" cy=\"12\" r=\"2\"></circle><path d=\"M16.2 7.8c2.3 2.3 2.3 6.1 0 8.5\"></path><path d=\"M19.1 4.9C23 8.8 23 15.1 19.1 19\"></path>",
        [IconNames.Headphones] = "<path d=\"M3 14h3a2 2 0 0 1 2 2v3a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-7a9 9 0 0 1 18 0v7a2 2 0 0 1-2 2h-1a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h3\"></path>",
        [IconNames.ThumbsUp] = "<path d=\"M7 10v12\"></path><path d=\"M15 5.88 14 10h5.83a2 2 0 0 1 1.92 2.56l-2.33 8A2 2 0 0 1 17.5 22H4a2 2 0 0 1-2-2v-8a2 2 0 0 1 2-2h3\"></path>",
        [IconNames.ThumbsDown] = "<path d=\"M17 14V2\"></path><path d=\"M9 18.12 10 14H4.17a2 2 0 0 1-1.92-2.56l2.33-8A2 2 0 0 1 6.5 2H20a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2h-3\"></path>",
        [IconNames.Heart] = "<path d=\"M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z\"></path>",
        [IconNames.Smile] = "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><path d=\"M8 14s1.5 2 4 2 4-2 4-2\"></path><line x1=\"9\" x2=\"9.01\" y1=\"9\" y2=\"9\"></line><line x1=\"15\" x2=\"15.01\" y1=\"9\" y2=\"9\"></line>",
        [IconNames.Frown] = "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle><path d=\"M16 16s-1.5-2-4-2-4 2-4 2\"></path><line x1=\"9\" x2=\"9.01\" y1=\"9\" y2=\"9\"></line><line x1=\"15\" x2=\"15.01\" y1=\"9\" y2=\"9\"></line>",

        // Commerce & Finance
        [IconNames.ShoppingCart] = "<circle cx=\"8\" cy=\"21\" r=\"1\"></circle><circle cx=\"19\" cy=\"21\" r=\"1\"></circle><path d=\"M2.05 2.05h2l2.66 12.42a2 2 0 0 0 2 1.58h9.78a2 2 0 0 0 1.95-1.57l1.65-7.43H5.12\"></path>",
        [IconNames.ShoppingBag] = "<path d=\"M6 2 3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4Z\"></path><line x1=\"3\" x2=\"21\" y1=\"6\" y2=\"6\"></line><path d=\"M16 10a4 4 0 0 1-8 0\"></path>",
        [IconNames.DollarSign] = "<line x1=\"12\" x2=\"12\" y1=\"2\" y2=\"22\"></line><path d=\"M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6\"></path>",
        [IconNames.Euro] = "<path d=\"M4 10h12\"></path><path d=\"M4 14h9\"></path><path d=\"M19 6a7.7 7.7 0 0 0-5.2-2A7.9 7.9 0 0 0 6 12c0 4.4 3.5 8 7.8 8 2 0 3.8-.8 5.2-2\"></path>",
        [IconNames.Wallet] = "<path d=\"M21 12V7H5a2 2 0 0 1 0-4h14v4\"></path><path d=\"M3 5v14a2 2 0 0 0 2 2h16v-5\"></path><path d=\"M18 12a2 2 0 0 0 0 4h4v-4Z\"></path>",
        [IconNames.Receipt] = "<path d=\"M4 2v20l2-1 2 1 2-1 2 1 2-1 2 1 2-1 2 1V2l-2 1-2-1-2 1-2-1-2 1-2-1-2 1-2-1Z\"></path><path d=\"M16 8h-6a2 2 0 1 0 0 4h4a2 2 0 1 1 0 4H8\"></path><path d=\"M12 17.5v-11\"></path>",
        [IconNames.TrendingUp] = "<polyline points=\"22 7 13.5 15.5 8.5 10.5 2 17\"></polyline><polyline points=\"16 7 22 7 22 13\"></polyline>",
        [IconNames.TrendingDown] = "<polyline points=\"22 17 13.5 8.5 8.5 13.5 2 7\"></polyline><polyline points=\"16 17 22 17 22 11\"></polyline>",
        [IconNames.BarChart] = "<line x1=\"12\" x2=\"12\" y1=\"20\" y2=\"10\"></line><line x1=\"18\" x2=\"18\" y1=\"20\" y2=\"4\"></line><line x1=\"6\" x2=\"6\" y1=\"20\" y2=\"16\"></line>",
        [IconNames.BarChart2] = "<line x1=\"18\" x2=\"18\" y1=\"20\" y2=\"10\"></line><line x1=\"12\" x2=\"12\" y1=\"20\" y2=\"4\"></line><line x1=\"6\" x2=\"6\" y1=\"20\" y2=\"14\"></line>",
        [IconNames.PieChart] = "<path d=\"M21.21 15.89A10 10 0 1 1 8 2.83\"></path><path d=\"M22 12A10 10 0 0 0 12 2v10z\"></path>",
        [IconNames.Activity] = "<polyline points=\"22 12 18 12 15 21 9 3 6 12 2 12\"></polyline>",

        // Security & User Management
        [IconNames.Shield] = "<path d=\"M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z\"></path>",
        [IconNames.ShieldCheck] = "<path d=\"M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z\"></path><path d=\"m9 12 2 2 4-4\"></path>",
        [IconNames.ShieldAlert] = "<path d=\"M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z\"></path><line x1=\"12\" x2=\"12\" y1=\"8\" y2=\"12\"></line><line x1=\"12\" x2=\"12.01\" y1=\"16\" y2=\"16\"></line>",
        [IconNames.Key] = "<circle cx=\"7.5\" cy=\"15.5\" r=\"5.5\"></circle><path d=\"m21 2-9.6 9.6\"></path><path d=\"m15.5 7.5 3 3L22 7l-3-3\"></path>",
        [IconNames.Unlock] = "<rect width=\"18\" height=\"11\" x=\"3\" y=\"11\" rx=\"2\" ry=\"2\"></rect><path d=\"M7 11V7a5 5 0 0 1 9.9-1\"></path>",
        [IconNames.Power] = "<path d=\"M18.36 6.64a9 9 0 1 1-12.73 0\"></path><line x1=\"12\" x2=\"12\" y1=\"2\" y2=\"12\"></line>",
        [IconNames.LogOut] = "<path d=\"M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4\"></path><polyline points=\"16 17 21 12 16 7\"></polyline><line x1=\"21\" x2=\"9\" y1=\"12\" y2=\"12\"></line>",
        [IconNames.LogIn] = "<path d=\"M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4\"></path><polyline points=\"10 17 15 12 10 7\"></polyline><line x1=\"15\" x2=\"3\" y1=\"12\" y2=\"12\"></line>",
        [IconNames.Zap] = "<polygon points=\"13 2 3 14 12 14 11 22 21 10 12 10 13 2\"></polygon>",
        [IconNames.Flame] = "<path d=\"M8.5 14.5A2.5 2.5 0 0 0 11 12c0-1.38-.5-2-1-3-1.072-2.143-.224-4.054 2-6 .5 2.5 2 4.9 4 6.5 2 1.6 3 3.5 3 5.5a7 7 0 1 1-14 0c0-1.153.433-2.294 1-3a2.5 2.5 0 0 0 2.5 2.5z\"></path>",
        [IconNames.Package] = "<line x1=\"16.5\" x2=\"7.5\" y1=\"9.4\" y2=\"4.21\"></line><path d=\"M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z\"></path><polyline points=\"3.27 6.96 12 12.01 20.73 6.96\"></polyline><line x1=\"12\" x2=\"12\" y1=\"22.08\" y2=\"12\"></line>",
        [IconNames.Box] = "<path d=\"M21 8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16Z\"></path><path d=\"m3.3 7 8.7 5 8.7-5\"></path><path d=\"M12 22V12\"></path>",
        [IconNames.Truck] = "<rect width=\"16\" height=\"12\" x=\"1\" y=\"3\" rx=\"2\"></rect><circle cx=\"5.5\" cy=\"18.5\" r=\"2.5\"></circle><circle cx=\"18.5\" cy=\"18.5\" r=\"2.5\"></circle><path d=\"M17 15h4l3-4v-5h-7Z\"></path>",
        [IconNames.Users] = "<path d=\"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2\"></path><circle cx=\"9\" cy=\"7\" r=\"4\"></circle><path d=\"M22 21v-2a4 4 0 0 0-3-3.87\"></path><path d=\"M16 3.13a4 4 0 0 1 0 7.75\"></path>",
        [IconNames.UserPlus] = "<path d=\"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2\"></path><circle cx=\"9\" cy=\"7\" r=\"4\"></circle><line x1=\"19\" x2=\"19\" y1=\"8\" y2=\"14\"></line><line x1=\"22\" x2=\"16\" y1=\"11\" y2=\"11\"></line>",
        [IconNames.UserMinus] = "<path d=\"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2\"></path><circle cx=\"9\" cy=\"7\" r=\"4\"></circle><line x1=\"22\" x2=\"16\" y1=\"11\" y2=\"11\"></line>",
        [IconNames.UserCheck] = "<path d=\"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2\"></path><circle cx=\"9\" cy=\"7\" r=\"4\"></circle><polyline points=\"16 11 18 13 22 9\"></polyline>",
        [IconNames.UserX] = "<path d=\"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2\"></path><circle cx=\"9\" cy=\"7\" r=\"4\"></circle><line x1=\"17\" x2=\"22\" y1=\"8\" y2=\"13\"></line><line x1=\"22\" x2=\"17\" y1=\"8\" y2=\"13\"></line>",

        // Media Playback Controls
        [IconNames.SkipBack] = "<polygon points=\"19 20 9 12 19 4 19 20\"></polygon><line x1=\"5\" x2=\"5\" y1=\"19\" y2=\"4\"></line>",
        [IconNames.SkipForward] = "<polygon points=\"5 4 15 12 5 20 5 4\"></polygon><line x1=\"19\" x2=\"19\" y1=\"5\" y2=\"20\"></line>",
        [IconNames.Shuffle] = "<polyline points=\"16 3 21 3 21 8\"></polyline><line x1=\"4\" x2=\"21\" y1=\"20\" y2=\"3\"></line><polyline points=\"21 16 21 21 16 21\"></polyline><line x1=\"15\" x2=\"21\" y1=\"15\" y2=\"21\"></line><line x1=\"4\" x2=\"9\" y1=\"4\" y2=\"9\"></line>",
        [IconNames.Repeat] = "<polyline points=\"17 1 21 5 17 9\"></polyline><path d=\"M3 11V9a4 4 0 0 1 4-4h14\"></path><polyline points=\"7 23 3 19 7 15\"></polyline><path d=\"M21 13v2a4 4 0 0 1-4 4H3\"></path>"
    };

    private static readonly Dictionary<string, string> s_allVariants = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> s_canonicalNames = new(StringComparer.OrdinalIgnoreCase);

    static LucideIcons()
    {
        // 1. Seed base paths
        foreach (var (k, v) in SvgPaths)
        {
            s_allVariants[k] = v;
            s_allVariants[k.Replace("-", "")] = v;
            s_canonicalNames[k] = k;
            s_canonicalNames[k.Replace("-", "")] = k;
        }

        // 2. Discover and register all fields from IconNames (PascalCase, prefixed, etc.)
        var fields = typeof(IconNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

        foreach (var field in fields)
        {
            var fieldName = field.Name;
            var constValue = (string)field.GetRawConstantValue()!;

            if (SvgPaths.TryGetValue(constValue, out var svg))
            {
                s_allVariants[fieldName] = svg;
                s_allVariants[$"IconNames.{fieldName}"] = svg;
                s_allVariants[$"@IconNames.{fieldName}"] = svg;
                s_allVariants[fieldName.ToLowerInvariant()] = svg;

                s_canonicalNames[fieldName] = constValue;
                s_canonicalNames[$"IconNames.{fieldName}"] = constValue;
                s_canonicalNames[$"@IconNames.{fieldName}"] = constValue;
                s_canonicalNames[fieldName.ToLowerInvariant()] = constValue;
            }
        }
    }

    /// <summary>
    /// Resolves an icon name from any notation (e.g. "IconNames.ChevronDown", "ChevronDown", "chevrondown", "chevron-down")
    /// to its canonical Lucide icon identifier (e.g. "chevron-down").
    /// </summary>
    public static string ResolveIconName(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return string.Empty;
        var clean = rawName.Trim();

        if (clean.StartsWith("@IconNames.", StringComparison.OrdinalIgnoreCase))
            clean = clean[11..];
        else if (clean.StartsWith("IconNames.", StringComparison.OrdinalIgnoreCase))
            clean = clean[10..];
        else if (clean.StartsWith("@", StringComparison.Ordinal))
            clean = clean[1..];

        if (s_canonicalNames.TryGetValue(clean, out var canonical))
            return canonical;

        var noHyphens = clean.Replace("-", "").Replace("_", "");
        if (s_canonicalNames.TryGetValue(noHyphens, out canonical))
            return canonical;

        return clean.ToLowerInvariant();
    }

    /// <summary>
    /// Returns the SVG inner elements for the given icon name.
    /// Resilient to prefixed, PascalCase, kebab-case, or un-hyphenated names.
    /// </summary>
    public static string GetPath(string? iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
            return "<circle cx=\"12\" cy=\"12\" r=\"10\"></circle>";

        var clean = iconName.Trim();
        if (clean.StartsWith("@IconNames.", StringComparison.OrdinalIgnoreCase))
            clean = clean[11..];
        else if (clean.StartsWith("IconNames.", StringComparison.OrdinalIgnoreCase))
            clean = clean[10..];
        else if (clean.StartsWith("@", StringComparison.Ordinal))
            clean = clean[1..];

        if (s_allVariants.TryGetValue(clean, out var path))
            return path;

        var normalized = clean.Replace("-", "").Replace("_", "");
        if (s_allVariants.TryGetValue(normalized, out path))
            return path;

        // Fallback placeholder circle
        return $"<path data-name=\"{iconName}\"/><circle cx=\"12\" cy=\"12\" r=\"10\"></circle>";
    }
}
