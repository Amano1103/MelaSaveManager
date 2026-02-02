# MELA Save Manager<br>
<img width="684" height="461" alt="MELA Save Manager 2026_02_03 4_26_34" src="https://github.com/user-attachments/assets/82287836-1989-4bd2-af44-fdc8e8da8a97" /><br><br>

MELA Save Manager は、VRChatのワールド「Memena's Lord Arena (MELA)」のバックアップコードを自動で検出し、保存・管理するためのツールです。プレイ中にログをリアルタイムで監視し、バックアップコードが生成されるたびに自動でローカルに保存します。<br><br>

## ✨ 特徴<br>
### ⚡リアルタイム自動保存<br>
・VRChatの output_log を常時監視し、バックアップコードが出力された瞬間に保存します。<br>
・ツールを起動しておくだけで、手動でログを漁る必要がなくなります。<br>
### 📅日付ごとの管理<br>
・保存されたコードは日付ごとに整理され、リスト形式で簡単に閲覧できます。<br>
・実際のテキストファイルも SaveCode/YYYY-MM-DD.txt の形式で自動生成されます。<br>
### 📋ワンクリック・コピー<br>
・リストに表示された時間をクリックするだけで、バックアップコードがクリップボードにコピーされます。<br><br>

## 📥 インストールと起動<br>
### １．ダウンロード<br>
・Releasesページから最新の MelaSaveManager.zip をダウンロードし、解凍してください。<br>
### ２．起動<br>
・フォルダ内の MelaSaveManager.exe を実行します。<br>
### ３．VRChatをプレイ<br>
・ツールを起動した状態でVRChatをプレイし、MELAワールドでセーブコードを生成してください。<br>
・自動的にリストに追加されます。<br><br>

## 📂 保存場所について<br>
本ツールは、実行ファイルと同じ階層に SaveCode というフォルダを自動作成し、その中にテキストファイルとしてデータを保存します。<br><br>

MelaSaveManager/<br>
  ├── MelaSaveManager.exe<br>
  ├── app.ico<br>
  └── SaveCode/          <-- ここに保存されます<br>
       ├── 2026-02-03.txt<br>
       ├── 2026-02-04.txt<br>
       └── ...<br><br>

## 🛠️ 動作環境<br>
・Windows 10 / 11<br>
・.NET Desktop Runtime<br>
・VRChat (PC Desktop / PCVR)<br><br>

## 開発技術<br>
・Language: C#<br>
・Framework: Windows Forms (.NET)<br>
・IDE: Visual Studio Code<br><br>

## ⚠️注意事項<br>
・このツールは非公式のファンメイドツールです。ワールド作者様およびVRChat公式とは関係ありません。<br>
・VRChatのログファイルの仕様変更等により、正常に動作しなくなる可能性があります。<br><br>

## License<br>
MIT License
