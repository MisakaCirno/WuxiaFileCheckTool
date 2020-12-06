using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace 天刀客户端自助修复工具
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 本地的检测结果
        /// </summary>
        public static Dictionary<string, string> allFilePathAndMD5 = new Dictionary<string, string>();

        /// <summary>
        /// 导入的检测结果
        /// </summary>
        public static Dictionary<string, string> allFilePathAndMD5Import = new Dictionary<string, string>();

        /// <summary>
        /// 所有要检测的子路径名
        /// </summary>
        private readonly List<string> foldersNeedToCheck = new List<string>()
        {
            //测试用路径
            /*
            $"\\Data",
            $"\\Data_x64",
            $"\\Dll_vc15",
            $"\\DLL_X64",
            */

            $"",
            $"\\Cache",
            $"\\Data",
            $"\\Data_x64",
            $"\\Dll_vc15",
            $"\\DLL_X64",
            $"\\Microsoft.VC80.CRT",
            //$"\\QBrowser",
            $"\\SFC",
            $"\\TCLS",
            //$"\\TenProtect64",
            //$"\\tqm",
            //$"\\TQM64",
            //$"\\WebBrowser",
            //$"\\WeGameLauncher",
            $"\\Win7",
            $"\\XVersion",
        };

        /// <summary>
        /// 出错的文件信息
        /// </summary>
        //private string errorFiles;

        /// <summary>
        /// 总文件数量
        /// </summary>
        private int allFilesCount;

        /// <summary>
        /// 已处理的文件数量
        /// </summary>
        private int workDoneCount;

        /// <summary>
        /// 选择的文件路径（天刀的根路径）
        /// </summary>
        private string parentPath;

        public MainWindow()
        {
            InitializeComponent();

            import_Button.IsEnabled = false;
            export_Button.IsEnabled = false;
        }

        //选择文件路径
        private void selectPath_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.FileName = "WuXia.exe";
            openFileDialog.DefaultExt = "WuXia.exe";
            openFileDialog.Filter = "天涯明月刀OL主程序|WuXia.exe";

            if (openFileDialog.ShowDialog() == true)
            {
                path_TextBox.Text = openFileDialog.FileName.Replace("\\WuXia.exe", string.Empty);

                import_Button.IsEnabled = false;
                export_Button.IsEnabled = false;
            }
        }

        private void Help_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "如何找到天刀目录：\r\n" +
                "1.启动Wegame\r\n" +
                "2.在【我的应用】中找到【天涯明月刀】，点击【右键】\r\n" +
                "3.选择【目录】\r\n" +
                "4.将弹出的【文件夹的路径】复制到本工具的【路径文本框】中即可\r\n" +
                "\r\n" +
                "如何使用：\r\n" +
                "在检测结果中：\r\n" +
                "“缺失文件”代表：此文件在正常客户端中存在，而在异常客户端中不存在。\r\n" +
                "“不同文件”代表：此文件在正常客户端中与异常客户端中不同。\r\n" +
                "只需要让有正常客户端的好友将正常的文件通过QQ等方式发给你，然后替换掉对应文件即可。\r\n" +
                "（注：游戏内出现的问题，主要可能源自于SFC文件的异常）\r\n" +
                "【建议在替换前备份原文件，以保证在出错的情况下可以还原。】\r\n" +
                "【如果本工具不能解决问题，建议从官网重新下载安装客户端，不要通过Wegame下载（Wegame的版本可能会比较老，下载后仍需更新多次）。】\r\n" +
                "\r\n" +
                "工具原理：\r\n" +
                "获取两个客户端的MD5后，进行对比，从而判断客户端的异常信息。\r\n",
                "工具使用帮助", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void About_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            new AboutWindow().ShowDialog();
        }

        //导入后自动开始对比
        private void Import_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.FileName = "MoonlightBlade_FilesMD5Data.json";
            openFileDialog.DefaultExt = "Json文件 (.json)|*.json";
            openFileDialog.Filter = "Json文件 (.json)|*.json";

            if (openFileDialog.ShowDialog() == true)
            {
                string openPath = openFileDialog.FileName;
                string fileData = File.ReadAllText(openPath);
                try
                {
                    allFilePathAndMD5Import.Clear();
                    allFilePathAndMD5Import = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileData);
                }
                catch (Exception)
                {
                    MessageBox.Show("导入的文件不正确！请重新确认导入的文件是否为本工具导出的文件！", "出错啦！");
                    return;
                }
            }
            else
            {
                return;
            }

            ObservableCollection<FileState> result = new ObservableCollection<FileState>();

            //开始对比
            foreach (var item in allFilePathAndMD5Import)
            {
                //如果同路径文件不存在，那么是文件缺失
                if (allFilePathAndMD5.ContainsKey(item.Key) == false)
                {
                    result.Add(new FileState() { FilePath = item.Key, State = "缺失文件" });
                }
                //如果同路径文件的MD5不同，那么文件是不同的
                else if (allFilePathAndMD5[item.Key] != item.Value)
                {
                    result.Add(new FileState() { FilePath = item.Key, State = "不同文件" });
                }
            }

            ResultWindow resultWindow = new ResultWindow(result);
            resultWindow.Show();
        }

        //导出分析结果
        private void Export_Button_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.FileName = "MoonlightBlade_FilesMD5Data.json";
            saveFileDialog.DefaultExt = "Json文件 (.txt)|*.json";
            saveFileDialog.Filter = "Json文件 (.txt)|*.json";

            string savePath;
            if (saveFileDialog.ShowDialog() == true)
            {
                savePath = saveFileDialog.FileName;
            }
            else
            {
                return;
            }

            File.WriteAllText(savePath, JsonConvert.SerializeObject(allFilePathAndMD5));
        }

        //检查文件
        private void CheckFile_Button_Click(object sender, RoutedEventArgs e)
        {
            //判断文件是否存在
            if (File.Exists($"{path_TextBox.Text}\\WuXia.exe") == false)
            {
                MessageBox.Show("天刀安装目录错误！请检查后重试！", "出错啦！");

                return;
            }

            //判断进程是否存在
            if (GetGameState())
            {
                MessageBox.Show("检测到游戏正在运行中！为了避免可能出现的问题，请退出游戏后再继续！", "出错啦！");
                return;
            }

            parentPath = path_TextBox.Text;

            //开始获取信息
            //设置IsBackground可以保证退出的时候线程也关闭
            new Thread(GetAllFilesData) { IsBackground = true }.Start();
        }

        /// <summary>
        /// 判断游戏进程是否存在
        /// </summary>
        /// <returns></returns>
        private bool GetGameState()
        {
            //WuXia_Client.exe
            //GameFileSystem.exe
            //
            //WuXia_Client_x64.exe
            //GameFileSystem_x64.exe
            //
            //WuXia_Client_dx12.exe

            System.Diagnostics.Process[] processList = System.Diagnostics.Process.GetProcesses();

            foreach (System.Diagnostics.Process process in processList)
            {
                switch (process.ProcessName)
                {
                    case "WuXia_Client":
                    case "GameFileSystem":
                    case "WuXia_Client_x64":
                    case "GameFileSystem_x64":
                    case "WuXia_Client_dx12":
                        return true;

                    default:
                        break;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取全部文件的信息
        /// </summary>
        private void GetAllFilesData()
        {
            allFilesCount = 0;
            workDoneCount = 0;
            //errorFiles = string.Empty;

            allFilePathAndMD5.Clear();

            var time = new Stopwatch();
            time.Start();

            Dispatcher.Invoke(() =>
            {
                checkFile_TextBlock.Text = "正在获取文件列表...";
            });

            //拼接完整的目录
            var foldersNeedToCheckWithFullPath = new List<string>();
            foreach (var folderName in foldersNeedToCheck)
            {
                foldersNeedToCheckWithFullPath.Add(parentPath + folderName);
            }

            //对文件进行计数，并整理出全部的路径
            var allFilePaths = new List<string>();
            foreach (var folderPath in foldersNeedToCheckWithFullPath)
            {
                if (folderPath == parentPath)
                {
                    //对于根目录，只需要统计其下的文件
                    GetFilePath(folderPath, false, allFilePaths);
                }
                else
                {
                    //对于其他目录，需要统计其内部的全部文件
                    GetFilePath(folderPath, true, allFilePaths);
                }
            }
            allFilesCount = allFilePaths.Count;

            Dispatcher.Invoke(() =>
            {
                progressBar.Value = 0;
                progressBar.Maximum = allFilesCount;
                checkFile_TextBlock.Text = $"统计完毕 开始记录文件特征...";
            });

            //依次处理每个路径中的文件
            foreach (var filePath in allFilePaths)
            {
                workDoneCount++;

                Dispatcher.Invoke(() =>
                {
                    progressBar.Value = workDoneCount;
                });

                Dispatcher.Invoke(() =>
                {
                    checkFile_TextBlock.Text = $"正在处理第{workDoneCount}个文件 共{allFilesCount}个文件 处理大文件时需要较长时间 请耐心等待...";
                });

                using (var md5 = MD5.Create())
                {
                    var fileMD5 = CalculateMD5(filePath, md5);

                    var relativePath = filePath.Replace(parentPath, "[天刀根目录]");
                    allFilePathAndMD5.Add(relativePath, fileMD5);
                }
            }

            //File.WriteAllText(Environment.CurrentDirectory + "\\errorFiles.txt", errorFiles);

            time.Stop();

            Dispatcher.Invoke(() =>
            {
                progressBar.Value = progressBar.Maximum;
                checkFile_TextBlock.Text = $"{workDoneCount}个文件已处理完成，耗时{time.Elapsed.TotalSeconds}秒";

                import_Button.IsEnabled = true;
                export_Button.IsEnabled = true;
            });
        }

        /// <summary>
        /// 获取某路径下的文件
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <param name="isGetSubFolder">是否获取子文件夹内的文件</param>
        /// <param name="result">要存储结果的List</param>
        private void GetFilePath(string folderPath, bool isGetSubFolder, List<string> result)
        {
            DirectoryInfo infos = new DirectoryInfo(folderPath);

            //获取全部文件
            foreach (var file in infos.GetFiles())
            {
                if (file.Length > 0)
                {
                    result.Add(file.FullName);
                }
            }

            //如果需要 就获取全部子文件的文件
            if (isGetSubFolder)
            {
                foreach (var directory in infos.GetDirectories())
                {
                    GetFilePath(directory.FullName, true, result);
                }
            }
        }

        /// <summary>
        /// 获取文件的MD5
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="md5Tool">在外部using的md5实例</param>
        /// <returns></returns>
        private string CalculateMD5(string filePath, MD5 md5Tool)
        {
            using (var stream = File.OpenRead(filePath))
            {
                var hash = md5Tool.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    /// <summary>
    /// 用于存储对比结果的类
    /// </summary>
    public class FileState
    {
        public string FilePath { get; set; }
        public string State { get; set; }
    }
}