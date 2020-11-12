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

namespace 天刀文件校验工具
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> tdFolders = new List<string>();

        public static List<Dictionary<string, string>> allFilePathAndMD5 = new List<Dictionary<string, string>>();

        public static List<Dictionary<string, string>> allFilePathAndMD5Import = new List<Dictionary<string, string>>();

        string errorFiles = string.Empty;

        int allFilesCount = 0;
        int workDoneCount = 0;

        string path;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void selectPath_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.FileName="WuXia.exe";
            openFileDialog.DefaultExt = "WuXia.exe"; // Default file extension
            openFileDialog.Filter = "天涯明月刀OL主程序|WuXia.exe"; // Filter files by extension

            if (openFileDialog.ShowDialog()==true)
            {
                path_TextBox.Text = openFileDialog.FileName.Replace("\\WuXia.exe", string.Empty);
            }
        }

        private void help_Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "1.启动Wegame\r\n" +
                "2.在【我的应用】中找到【天涯明月刀】，点击【右键】\r\n" +
                "3.选择【目录】\r\n" +
                "4.将弹出的【文件夹的路径】复制到本工具的【路径文本框】中即可\r\n",
                "如何找到天刀安装目录");
        }

        private void checkFile_Button_Click(object sender, RoutedEventArgs e)
        {
            path = path_TextBox.Text;
            new Thread(GetAllFile).Start();
        }

        private void GetAllFile()
        {
            allFilesCount = 0;
            workDoneCount = 0;
            errorFiles = string.Empty;

            var time = new Stopwatch();
            time.Start();

            if (File.Exists($"{path}\\WuXia.exe"))
            {
                //添加所有的文件夹路径
                tdFolders.Add($"{path}");
                tdFolders.Add($"{path}\\Cache");
                tdFolders.Add($"{path}\\Data");
                tdFolders.Add($"{path}\\Data_x64");
                tdFolders.Add($"{path}\\Dll_vc15");
                tdFolders.Add($"{path}\\DLL_X64");
                tdFolders.Add($"{path}\\Microsoft.VC80.CRT");
                //tdFolders.Add($"{path}\\QBrowser");
                tdFolders.Add($"{path}\\SFC");
                tdFolders.Add($"{path}\\TCLS");
                //tdFolders.Add($"{path}\\TenProtect64");
                //tdFolders.Add($"{path}\\tqm");
                //tdFolders.Add($"{path}\\TQM64");
                //tdFolders.Add($"{path}\\WebBrowser");
                //tdFolders.Add($"{path}\\WeGameLauncher");
                tdFolders.Add($"{path}\\Win7");
                tdFolders.Add($"{path}\\XVersion");

                Dispatcher.Invoke(()=> 
                { 
                    checkFile_TextBlock.Text = "正在获取文件列表..."; 
                });

                foreach (var folderPath in tdFolders)
                {
                    if (folderPath == path)
                    {
                        allFilesCount += Directory.GetFiles(folderPath, "*", SearchOption.TopDirectoryOnly).Length;
                    }
                    else
                    {
                        allFilesCount += Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories).Length; ;
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    checkFile_TextBlock.Text = $"开始记录文件特征...";
                });

                //依次处理每个路径中的文件
                foreach (var folderPath in tdFolders)
                {
                    if (folderPath == path)
                    {
                        Dictionary<string, string> result = new Dictionary<string, string>();
                        GetAllFilePathAndMD5InFolder(folderPath, false, result);
                        allFilePathAndMD5.Add(result) ;
                    }
                    else
                    {
                        Dictionary<string, string> result = new Dictionary<string, string>();
                        GetAllFilePathAndMD5InFolder(folderPath, true, result);
                        allFilePathAndMD5.Add(result);
                    }
                }

                File.WriteAllText(Environment.CurrentDirectory+"\\errorFiles.txt", errorFiles);

                time.Stop();

                Dispatcher.Invoke(() =>
                {
                    checkFile_TextBlock.Text = $"{workDoneCount}个文件已处理完成，{allFilesCount-workDoneCount}个文件被忽略，耗时{time.Elapsed.TotalSeconds}秒";
                });
            }
            else
            {
                MessageBox.Show("天刀目录错误！请检查后重试！", "出错啦！");
            }
        }

        /// <summary>
        /// 获取某路径中全部的文件的文件路径和MD5值
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="isGetSubfolder"></param>
        private void GetAllFilePathAndMD5InFolder(string folder,bool isGetSubfolder, Dictionary<string, string> targetDic)
        {
            DirectoryInfo infos = new DirectoryInfo(folder);

            //获取全部文件
            foreach (var file in infos.GetFiles())
            {
                using (var md5 = MD5.Create())
                {
                    if (file.Length>0)
                    {
                        var fileMd5 = CalculateMD5(file.FullName, md5);

                        if (targetDic.ContainsKey(fileMd5))
                        {
                            errorFiles += $"{targetDic[fileMd5]}\t{file.FullName}";
                        }
                        else
                        {
                            targetDic.Add(fileMd5, file.FullName);

                            workDoneCount++;

                            Dispatcher.Invoke(() =>
                            {
                                checkFile_TextBlock.Text = $"已处理{workDoneCount}个文件，共{allFilesCount}个";
                            });
                        }
                    }
                }
            }

            //获取全部子文件的文件
            if (isGetSubfolder)
            {
                foreach (var directory in infos.GetDirectories())
                {
                    GetAllFilePathAndMD5InFolder(directory.FullName,true, targetDic);
                }
            }
        }

        /// <summary>
        /// 获取文件的MD5
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="md5tool"></param>
        /// <returns></returns>
        private string CalculateMD5(string filename,MD5 md5tool)
        {
            using (var stream = File.OpenRead(filename))
            {
                var hash = md5tool.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }


        private void import_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.FileName = "MoonlightBlade_FilesMD5Data.Json";
            openFileDialog.DefaultExt = "Json文件 (.txt)|*.Json";
            openFileDialog.Filter = "Json文件 (.txt)|*.Json";

            string openPath = string.Empty;

            if (openFileDialog.ShowDialog() == true)
            {
                openPath = openFileDialog.FileName;
                string fileData = File.ReadAllText(openPath);
                allFilePathAndMD5Import = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(fileData);
            }
            else
            {
                return;
            }

            ObservableCollection<FileState> result = new ObservableCollection<FileState>();

            //开始对比
            for (int i = 0; i < allFilePathAndMD5.Count; i++)
            {
                foreach (var item in allFilePathAndMD5Import[i])
                {
                    if (allFilePathAndMD5[i].ContainsKey(item.Key)==false)
                    {
                        result.Add(new FileState() { FilePath = item.Value, State = "文件缺失" });
                    }
                    else if (allFilePathAndMD5[i][item.Key]!=item.Value)
                    {
                        result.Add(new FileState() { FilePath = item.Value, State = "文件异常" });
                    }
                }
            }

            ResultWindow resultWindow = new ResultWindow(result);
            resultWindow.Show();
        }

        private void export_Button_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.FileName = "MoonlightBlade_FilesMD5Data.Json";
            saveFileDialog.DefaultExt = "Json文件 (.txt)|*.Json";
            saveFileDialog.Filter = "Json文件 (.txt)|*.Json";

            string savePath = string.Empty;

            if (saveFileDialog.ShowDialog()==true)
            {
                savePath = saveFileDialog.FileName;
            }
            else
            {
                return;
            }

            File.WriteAllText(savePath, JsonConvert.SerializeObject(allFilePathAndMD5));
        }
    }

    public class FileState
    {
        public string FilePath { get; set; }
        public string State { get; set; }
    }
}
