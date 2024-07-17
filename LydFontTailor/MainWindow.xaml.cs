using HandyControl.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml.Linq;

namespace LydFontTailor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            startBtn.Click += (s, e) =>
            {
                try
                {
                    startBtn.IsEnabled = false;
                    if (!VerifyFileFormat(sourcePath.Text) || !VerifyFileFormat(exportPath.Text))
                    {
                        HandyControl.Controls.Growl.Error("只支持.ttf,.otf,.woff,.woff2格式的文件", "FT");
                        return;
                    }

                    if (!File.Exists(sourcePath.Text))
                    {
                        HandyControl.Controls.Growl.Error("源字体文件不存在", "FT");
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(fontEnName.Text))
                    {
                        if(!Regex.IsMatch(fontEnName.Text, @"^[a-zA-Z]+$"))
                        {
                            HandyControl.Controls.Growl.Error("字体名需要英文", "FT");
                            return;
                        }
                    }

                    string family = Path.GetFileNameWithoutExtension(exportPath.Text);

                    char[] chars = reserveChar.Text.ToCharArray();
                    HashSet<int> unicodes = new HashSet<int>();
                    foreach (char c in chars)
                    {
                        int unicode = Convert.ToInt32(c);
                        if (!unicodes.Contains(unicode))
                            unicodes.Add(c);
                    }

                    string? directoryPath = Path.GetDirectoryName(exportPath.Text);
                    if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                        Directory.CreateDirectory(directoryPath);

                    //创建脚本
                    CreatePeScript1(sourcePath.Text, unicodes, exportPath.Text,fontEnName.Text,fontZhName.Text);
                    SetupFontForge();
                }
                catch(Exception ex)
                {
                    HandyControl.Controls.Growl.Error(ex.Message, "FT");
                }
                finally
                {
                    File.Delete("script.pe");
                    startBtn.IsEnabled = true;
                }
            };
        }

        //创建裁剪字体脚本
        private void CreatePeScript1(string source,HashSet<int> unicodes,string export,string enName,string zhName)
        {
            source = source.Replace(@"\", "/").Replace(@"\\", "/");
            export = export.Replace(@"\", "/").Replace(@"\\", "/");
            StringBuilder sb = new();
            sb.AppendLine($"Open(\"{source}\")");
            sb.AppendLine($"SelectNone()");

            foreach(int unicode in unicodes)
                sb.AppendLine($"SelectMore({unicode})");

            sb.AppendLine($"SelectInvert()");
            sb.AppendLine($"Clear()");

            if (!string.IsNullOrWhiteSpace(enName))
            {
                sb.AppendLine($"SetFontNames(\"{enName}\", \"{enName}\", \"{enName}\")");
                sb.AppendLine($"SetTTFName(0x409,1,\"{enName}\")");
                sb.AppendLine($"SetTTFName(0x409,3,\"{enName}\")");
                sb.AppendLine($"SetTTFName(0x409,4,\"{enName}\")");
            }

            if (!string.IsNullOrWhiteSpace(zhName))
            {
                sb.AppendLine($"SetTTFName(0x804,1,\"{zhName}\")");
                sb.AppendLine($"SetTTFName(0x804,3,\"{zhName}\")");
                sb.AppendLine($"SetTTFName(0x804,4,\"{zhName}\")");
            }


            sb.AppendLine($"Generate(\"{export}\")");
            sb.AppendLine($"Quit()");

            using StreamWriter writer = new("script.pe");
            writer.Write(sb.ToString());
        }
        private void SetupFontForge()
        {
            
            // 创建进程启动信息
            var startInfo = new ProcessStartInfo
            {
                FileName = "FontForge/bin/fontforge.exe",
                Arguments = $"-script {Path.GetFullPath("script.pe")}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            // 启动进程
            using var process = new Process { StartInfo = startInfo };
            process.Start();

            // 读取消息
            string msg = process.StandardError.ReadToEnd();

            process.WaitForExit();



            if(msg.Trim().EndsWith("a1dad3e81da03d5d5f3c4c1c1b9b5ca5ebcfcecf"))
                HandyControl.Controls.Growl.Success("成功", "FT");
            else
            {
                HandyControl.Controls.Growl.Error(msg.Split("a1dad3e81da03d5d5f3c4c1c1b9b5ca5ebcfcecf")[1], "FT");
            }
        }
        private bool VerifyFileFormat(string path)
        {
            bool b1 = path.EndsWith(".ttf", true, CultureInfo.CurrentCulture);
            bool b2 = path.EndsWith(".otf", true, CultureInfo.CurrentCulture);
            bool b3 = path.EndsWith(".woff", true, CultureInfo.CurrentCulture);
            bool b4 = path.EndsWith(".woff2", true, CultureInfo.CurrentCulture);

            return b1 || b2 || b3 || b4;
        }
    }
}