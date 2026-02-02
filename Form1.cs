using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace MelaSaveManager
{
    public class BackupData
    {
        public string FullTimestamp { get; set; } = ""; 
        public string Code { get; set; } = "";

        public string DatePart
        {
            get 
            {
                if (FullTimestamp.Length >= 10)
                    return FullTimestamp.Substring(0, 10);
                return "Unknown Date";
            }
        }

        public string TimePart
        {
            get
            {
                if (FullTimestamp.Length > 11)
                    return FullTimestamp.Substring(11);
                return FullTimestamp;
            }
        }

        public override string ToString()
        {
            return $"[{TimePart}]";
        }
    }

    public partial class Form1 : Form
    {
        private static readonly string LogDirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            @"AppData\LocalLow\VRChat\VRChat");

        private static readonly string SaveBaseDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "SaveCode");
        
        private SplitContainer splitContainer;
        private ListBox dateList;
        private ListBox timeList;

        private List<BackupData> allBackups = new List<BackupData>();
        private bool isMonitoring = true;

        public Form1()
        {
            this.Text = "MELA Save Manager";
            this.Size = new Size(700, 500);

            try
            {
                if (File.Exists("app.ico"))
                {
                    this.Icon = new Icon("app.ico");
                }
            }
            catch { }

            splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.SplitterWidth = 4;
            splitContainer.FixedPanel = FixedPanel.Panel1;
            splitContainer.SplitterDistance = 200; 
            this.Controls.Add(splitContainer);

            dateList = new ListBox();
            dateList.Dock = DockStyle.Fill;
            dateList.Font = new Font("Consolas", 12);
            dateList.BackColor = Color.FromArgb(30, 30, 30);
            dateList.ForeColor = Color.White;
            dateList.SelectedIndexChanged += OnDateSelected; 
            splitContainer.Panel1.Controls.Add(dateList);

            timeList = new ListBox();
            timeList.Dock = DockStyle.Fill;
            timeList.Font = new Font("Consolas", 12);
            timeList.BackColor = Color.FromArgb(30, 30, 30);
            timeList.ForeColor = Color.White;
            timeList.SelectedIndexChanged += OnTimeSelected; 
            splitContainer.Panel2.Controls.Add(timeList);

            this.Load += OnFormLoad;
        }

        private void OnFormLoad(object? sender, EventArgs e)
        {
            if (!Directory.Exists(SaveBaseDirectory))
            {
                Directory.CreateDirectory(SaveBaseDirectory);
            }

            LoadSavedHistory();

            // 監視スタート（引数なしで呼び出し、自分自身でファイルを探させる）
            _ = MonitorLogFileAsync();
        }

        private void LoadSavedHistory()
        {
            allBackups.Clear();

            if (!Directory.Exists(SaveBaseDirectory)) return;

            string[] files = Directory.GetFiles(SaveBaseDirectory, "*.txt");

            foreach (string filePath in files)
            {
                try
                {
                    string allText = File.ReadAllText(filePath);
                    string[] blocks = allText.Split(new[] { "----------------------------------------" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string block in blocks)
                    {
                        string trimmed = block.Trim();
                        if (string.IsNullOrWhiteSpace(trimmed)) continue;

                        int firstLineEnd = trimmed.IndexOf('\n');
                        if (firstLineEnd > 0)
                        {
                            string timePart = trimmed.Substring(0, firstLineEnd).Trim();
                            string codePart = trimmed.Substring(firstLineEnd).Trim();
                            timePart = timePart.Replace("[", "").Replace("]", "");
                            allBackups.Add(new BackupData { FullTimestamp = timePart, Code = codePart });
                        }
                    }
                }
                catch { }
            }

            allBackups.Sort((a, b) => b.FullTimestamp.CompareTo(a.FullTimestamp));
            UpdateDateList();
        }

        private void UpdateDateList()
        {
            string? selectedDate = dateList.SelectedItem as string;
            dateList.Items.Clear();
            var uniqueDates = allBackups.Select(d => d.DatePart).Distinct().ToList();

            foreach (var date in uniqueDates)
            {
                dateList.Items.Add(date);
            }

            if (dateList.Items.Count > 0)
            {
                if (selectedDate != null && dateList.Items.Contains(selectedDate))
                {
                    dateList.SelectedItem = selectedDate;
                }
                else
                {
                    dateList.SelectedIndex = 0;
                }
            }
        }

        private void OnDateSelected(object? sender, EventArgs e)
        {
            if (dateList.SelectedItem == null) return;
            string selectedDate = dateList.SelectedItem.ToString() ?? "";
            timeList.Items.Clear();
            var filteredItems = allBackups.Where(d => d.DatePart == selectedDate).ToList();
            foreach (var item in filteredItems)
            {
                timeList.Items.Add(item);
            }
        }

        private void OnTimeSelected(object? sender, EventArgs e)
        {
            if (timeList.SelectedItem is BackupData selectedData)
            {
                try
                {
                    Clipboard.SetDataObject(selectedData.Code, true, 5, 200); 
                }
                catch (ExternalException) { }
            }
        }

        private void AddNewBackup(string code, string timestamp)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AddNewBackup(code, timestamp)));
                return;
            }

            if (allBackups.Any(b => b.Code == code))
            {
                return;
            }

            string datePart = "Unknown";
            if (timestamp.Length >= 10)
            {
                datePart = timestamp.Substring(0, 10);
                datePart = datePart.Replace("/", "-").Replace("\\", "-");
            }

            string fileName = $"{datePart}.txt";
            string filePath = Path.Combine(SaveBaseDirectory, fileName);

            try
            {
                if (!Directory.Exists(SaveBaseDirectory))
                {
                    Directory.CreateDirectory(SaveBaseDirectory);
                }
                string saveContent = $"[{timestamp}]\n{code}\n----------------------------------------\n";
                File.AppendAllText(filePath, saveContent);
            }
            catch { }

            var newData = new BackupData { FullTimestamp = timestamp, Code = code };
            allBackups.Insert(0, newData); 
            UpdateDateList(); 
            
            if (dateList.Items.Count > 0 && dateList.Items[0].ToString() == newData.DatePart)
            {
                dateList.SelectedIndex = 0; 
            }
        }

        private FileInfo? GetLatestLogFile()
        {
            if (!Directory.Exists(LogDirectoryPath)) return null;
            var dirInfo = new DirectoryInfo(LogDirectoryPath);
            // output_log_*.txt の中で一番更新日時が新しいものを取得
            return dirInfo.GetFiles("output_log_*.txt")
                          .OrderByDescending(f => f.LastWriteTime)
                          .FirstOrDefault();
        }

        // ▼ 変更: 自動ファイル切り替え機能を実装した監視ロジック
        private async Task MonitorLogFileAsync()
        {
            string pattern = @"\$\$MELA Achievements Backup:\$\$(.*?)\$\$Backup Over\$\$";
            Regex regex = new Regex(pattern, RegexOptions.Singleline);

            // 最初に現在の最新ログを取得
            FileInfo? currentFile = GetLatestLogFile();

            while (isMonitoring)
            {
                // ファイルが見つからない場合は待機して再検索
                if (currentFile == null)
                {
                    await Task.Delay(3000);
                    currentFile = GetLatestLogFile();
                    continue;
                }

                // 現在のファイルを開いて監視
                try
                {
                    using (FileStream fs = new FileStream(currentFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                    {
                        // ファイルが開けたら、中身を読み続けるループへ
                        while (isMonitoring)
                        {
                            string? line = await sr.ReadLineAsync();

                            if (line != null)
                            {
                                // データがある場合の処理（前回と同じ）
                                Match match = regex.Match(line);
                                if (match.Success)
                                {
                                    string timestamp = "";
                                    if (line.Length >= 19)
                                    {
                                        timestamp = line.Substring(0, 19).Replace(".", "-");
                                    }
                                    else
                                    {
                                        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                    }
                                    AddNewBackup(match.Groups[1].Value, timestamp);
                                }
                            }
                            else
                            {
                                // ファイルの末尾に到達した場合
                                // ここで「もっと新しいファイルができていないか？」をチェックします
                                await Task.Delay(1000);

                                FileInfo? latest = GetLatestLogFile();
                                if (latest != null && latest.FullName != currentFile.FullName)
                                {
                                    // ファイル名が違う＝新しいログファイルが生成された！
                                    // ターゲットを新しいファイルに切り替えるため、内側のループ(StreamReader)を抜ける
                                    currentFile = latest;
                                    break; 
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // ファイルが開けない場合などのエラー待機
                    await Task.Delay(1000);
                }
            }
        }
    }
}
