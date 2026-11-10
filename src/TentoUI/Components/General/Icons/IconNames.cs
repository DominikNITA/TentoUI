using TentoUI.Extensibility;

namespace TentoUI;

[Since("1.0.1")]
public enum IconSpinSpeed
{
    Slow,
    Normal,
    Fast
}

public static class IconNames
{
    public const string Check = "check";
    public const string CheckCircle = "check-circle";
    public const string CheckCircle2 = "check-circle-2";
    public const string AlertCircle = "alert-circle";
    public const string AlertTriangle = "alert-triangle";
    public const string Info = "info";
    public const string X = "x";
    public const string Close = "close";
    public const string XCircle = "x-circle";
    public const string Loader = "loader";
    public const string Loader2 = "loader-2";
    public const string ChevronDown = "chevron-down";
    public const string ChevronUp = "chevron-up";
    public const string ChevronLeft = "chevron-left";
    public const string ChevronRight = "chevron-right";
    public const string ArrowLeft = "arrow-left";
    public const string ArrowRight = "arrow-right";
    public const string ArrowUp = "arrow-up";
    public const string ArrowDown = "arrow-down";
    public const string Plus = "plus";
    public const string Minus = "minus";
    public const string Search = "search";
    public const string Copy = "copy";
    public const string Edit = "edit";
    public const string Edit2 = "edit-2";
    public const string Trash = "trash";
    public const string Trash2 = "trash-2";
    public const string ExternalLink = "external-link";
    public const string Link = "link";
    public const string Filter = "filter";
    public const string User = "user";
    public const string Calendar = "calendar";
    public const string Clock = "clock";
    public const string Sun = "sun";
    public const string Moon = "moon";
    public const string Menu = "menu";
    public const string Eye = "eye";
    public const string EyeOff = "eye-off";
    public const string RefreshCw = "refresh-cw";
    public const string HelpCircle = "help-circle";
    public const string Star = "star";
    public const string StarHalf = "star-half";
    public const string Sparkles = "sparkles";
    public const string Upload = "upload";
    public const string Download = "download";
    public const string Inbox = "inbox";
    public const string MoreHorizontal = "more-horizontal";
    public const string MoreVertical = "more-vertical";
    public const string Settings = "settings";
    public const string Bell = "bell";
    public const string Home = "home";
    public const string File = "file";
    public const string Folder = "folder";
    public const string Play = "play";
    public const string Pause = "pause";
    public const string Music = "music";
    public const string RotateCcw = "rotate-ccw";
    public const string RotateCw = "rotate-cw";
    public const string VolumeX = "volume-x";
    public const string Volume1 = "volume-1";
    public const string Volume2 = "volume-2";
    public const string Square = "square";
    public const string Building = "building";
    public const string Lock = "lock";
    public const string CreditCard = "credit-card";

    // Device & Hardware
    public const string Smartphone = "smartphone";
    public const string Tablet = "tablet";
    public const string Laptop = "laptop";
    public const string Monitor = "monitor";
    public const string Tv = "tv";
    public const string Printer = "printer";
    public const string HardDrive = "hard-drive";
    public const string Cpu = "cpu";
    public const string Server = "server";
    public const string Database = "database";
    public const string Wifi = "wifi";
    public const string WifiOff = "wifi-off";
    public const string Bluetooth = "bluetooth";
    public const string Battery = "battery";
    public const string BatteryCharging = "battery-charging";

    // Actions & Productivity
    public const string Save = "save";
    public const string Bookmark = "bookmark";
    public const string Pin = "pin";
    public const string Send = "send";
    public const string Share = "share";
    public const string Share2 = "share-2";
    public const string Terminal = "terminal";
    public const string Code = "code";
    public const string Code2 = "code-2";
    public const string Layers = "layers";
    public const string Sliders = "sliders";
    public const string Compass = "compass";
    public const string MapPin = "map-pin";
    public const string Navigation = "navigation";
    public const string Crosshair = "crosshair";
    public const string Globe = "globe";
    public const string Maximize = "maximize";
    public const string Minimize = "minimize";
    public const string Maximize2 = "maximize-2";
    public const string Minimize2 = "minimize-2";
    public const string ZoomIn = "zoom-in";
    public const string ZoomOut = "zoom-out";
    public const string History = "history";
    public const string Flag = "flag";
    public const string Tag = "tag";
    public const string Archive = "archive";

    // Communication, Social & Media
    public const string Mail = "mail";
    public const string MessageSquare = "message-square";
    public const string MessageCircle = "message-circle";
    public const string Phone = "phone";
    public const string PhoneCall = "phone-call";
    public const string PhoneOff = "phone-off";
    public const string Video = "video";
    public const string VideoOff = "video-off";
    public const string Mic = "mic";
    public const string MicOff = "mic-off";
    public const string Camera = "camera";
    public const string Image = "image";
    public const string Radio = "radio";
    public const string Headphones = "headphones";
    public const string ThumbsUp = "thumbs-up";
    public const string ThumbsDown = "thumbs-down";
    public const string Heart = "heart";
    public const string Smile = "smile";
    public const string Frown = "frown";

    // Commerce & Finance
    public const string ShoppingCart = "shopping-cart";
    public const string ShoppingBag = "shopping-bag";
    public const string DollarSign = "dollar-sign";
    public const string Euro = "euro";
    public const string Wallet = "wallet";
    public const string Receipt = "receipt";
    public const string TrendingUp = "trending-up";
    public const string TrendingDown = "trending-down";
    public const string BarChart = "bar-chart";
    public const string BarChart2 = "bar-chart-2";
    public const string PieChart = "pie-chart";
    public const string Activity = "activity";

    // Security & User Management
    public const string Shield = "shield";
    public const string ShieldCheck = "shield-check";
    public const string ShieldAlert = "shield-alert";
    public const string Key = "key";
    public const string Unlock = "unlock";
    public const string Power = "power";
    public const string LogOut = "log-out";
    public const string LogIn = "log-in";
    public const string Zap = "zap";
    public const string Flame = "flame";
    public const string Package = "package";
    public const string Box = "box";
    public const string Truck = "truck";
    public const string Users = "users";
    public const string UserPlus = "user-plus";
    public const string UserMinus = "user-minus";
    public const string UserCheck = "user-check";
    public const string UserX = "user-x";

    // Media Playback Controls
    public const string SkipBack = "skip-back";
    public const string SkipForward = "skip-forward";
    public const string Shuffle = "shuffle";
    public const string Repeat = "repeat";
}
