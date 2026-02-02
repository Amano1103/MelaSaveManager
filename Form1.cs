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
    // データ管理用クラス
    public class BackupData
    {
        public string FullTimestamp { get; set; } = ""; 
        public string Code { get; set; } = "";

        // 日付部分 (YYYY-MM-DD)
        public string DatePart
        {
            get 
            {
                if (FullTimestamp.Length >= 10)
                    return FullTimestamp.Substring(0, 10);
                return "Unknown Date";
            }
        }

        // 時間部分 (HH:mm:ss)
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
        // ----------------------------------------------------
        // 設定項目
        // ----------------------------------------------------
        private static readonly string LogDirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            @"AppData\LocalLow\VRChat\VRChat");

        // 保存先ディレクトリ (exeと同じ場所の "SaveCode" フォルダ)
        private static readonly string SaveBaseDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "SaveCode");
        
        // 画面パーツ
        private SplitContainer splitContainer;
        private ListBox dateList;     // 左側：日付リスト
        private ListBox timeList;     // 右側：時間リスト

        // データリスト
        private List<BackupData> allBackups = new List<BackupData>();
        private bool isMonitoring = true;

        public Form1()
        {
            // ----------------------------------------------------
            // GUI構築
            // ----------------------------------------------------
            this.Text = "MELA Save Manager";
            this.Size = new Size(700, 500);

            // ▼ 追加: ウィンドウ左上のアイコンを設定
            // 実行フォルダに app.ico があれば読み込む
            try
            {
                if (File.Exists("app.ico"))
                {
                    this.Icon = new Icon("app.ico");
                }
            }
            catch { /* アイコン読み込みエラーは無視 */ }

            // スプリットコンテナ (左右分割)
            splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.SplitterWidth = 4;
            splitContainer.FixedPanel = FixedPanel.Panel1;
            splitContainer.SplitterDistance = 200; 
            this.Controls.Add(splitContainer);

            // --- 左側 (日付リスト) ---
            dateList = new ListBox();
            dateList.Dock = DockStyle.Fill;
            dateList.Font = new Font("Consolas", 12);
            dateList.BackColor = Color.FromArgb(30, 30, 30);
            dateList.ForeColor = Color.White;
            dateList.SelectedIndexChanged += OnDateSelected; 
            splitContainer.Panel1.Controls.Add(dateList);

            // --- 右側 (時間リスト) ---
            timeList = new ListBox();
            timeList.Dock = DockStyle.Fill;
            timeList.Font = new Font("Consolas", 12);
            timeList.BackColor = Color.FromArgb(30, 30, 30);
            timeList.ForeColor = Color.White;
            timeList.SelectedIndexChanged += OnTimeSelected; 
            splitContainer.Panel2.Controls.Add(timeList);

            // 読み込みイベント
            this.Load += OnFormLoad;
        }

        private async void OnFormLoad(object? sender, EventArgs e)
        {
            // 起動時に SaveCode フォルダが無ければ作成
            if (!Directory.Exists(SaveBaseDirectory))
            {
                Directory.CreateDirectory(SaveBaseDirectory);
            }

            // 既存データを読み込み
            LoadSavedHistory();

            FileInfo? latestLog = GetLatestLogFile();
            if (latestLog != null)
            {
                await MonitorLogFileAsync(latestLog);
            }
        }

        // ----------------------------------------------------
        // ロジック
        // ----------------------------------------------------

        // フォルダ内の全ファイルを読み込む
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
                catch { /* 読み込みエラーは無視 */ }
            }

            // 日付順・時間順に並べ替え (新しい順)
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

        // ----------------------------------------------------
        // イベントハンドラ
        // ----------------------------------------------------

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

        // 日付ごとのファイルに保存
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

            // 日付部分を取得 (例: 2026-02-03)
            string datePart = "Unknown";
            if (timestamp.Length >= 10)
            {
                datePart = timestamp.Substring(0, 10);
                datePart = datePart.Replace("/", "-").Replace("\\", "-");
            }

            // 保存先ファイルパス: SaveCode/2026-02-03.txt
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
            catch { /* 無視 */ }

            // メモリ上のリストに追加して画面更新
            var newData = new BackupData { FullTimestamp = timestamp, Code = code };
            allBackups.Insert(0, newData); 

            UpdateDateList(); 
            
            if (dateList.Items.Count > 0 && dateList.Items[0].ToString() == newData.DatePart)
            {
                dateList.SelectedIndex = 0; 
            }
        }

        // ----------------------------------------------------
        // 監視ロジック
        // ----------------------------------------------------
        private FileInfo? GetLatestLogFile()
        {
            if (!Directory.Exists(LogDirectoryPath)) return null;
            var dirInfo = new DirectoryInfo(LogDirectoryPath);
            return dirInfo.GetFiles("output_log_*.txt")
                          .OrderByDescending(f => f.LastWriteTime)
                          .FirstOrDefault();
        }

        private async Task MonitorLogFileAsync(FileInfo fileInfo)
        {
            string pattern = @"\$\$MELA Achievements Backup:\$\$(.*?)\$\$Backup Over\$\$";
            Regex regex = new Regex(pattern, RegexOptions.Singleline);

            try
            {
                using (FileStream fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                {
                    while (isMonitoring)
                    {
                        string? line = await sr.ReadLineAsync();

                        if (line != null)
                        {
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
                            await Task.Delay(1000);
                        }
                    }
                }
            }
            catch { }
        }
    }
}