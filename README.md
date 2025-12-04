# リアルタイム音声文字起こしシステム

Azure Speech Serviceを使用したリアルタイム音声文字起こしと話者分離のデモアプリケーションです。

## ⚠️ 重要な注意事項

このシステムはデモンストレーション用です。音声認識および話者分離の結果には誤りが含まれる可能性があります。

## 機能

- 🎤 **リアルタイム音声文字起こし**: ブラウザマイクからの音声をリアルタイムで文字起こし
- 👥 **話者分離**: 停止後に高精度な話者分離を実行
- 📝 **速記者メモ**: 文字起こし中にメモを挿入可能
- 💾 **WebM録音**: バックアップとして音声をWebM形式で保存
- 🎨 **2カラムUI**: リアルタイム文字起こし(左)と話者分離結果(右)を同時表示

## 必要な環境

- .NET 8.0 SDK
- Azure Speech Serviceサブスクリプション
- モダンブラウザ(Chrome, Edge推奨)

## セットアップ

### 1. リポジトリのクローン

```bash
git clone <repository-url>
cd 20251204-speech2text
```

### 2. Azure Speech Serviceの設定

1. [Azure Portal](https://portal.azure.com)でSpeech Serviceリソースを作成
2. APIキーとエンドポイントを取得
3. 開発環境用の設定ファイルを作成:

```bash
cp appsettings.json appsettings.Development.json
```

4. `appsettings.Development.json`に実際の値を設定:

```json
{
  "AzureSpeech": {
    "Key": "YOUR_AZURE_SPEECH_KEY_HERE",
    "Endpoint": "https://YOUR_REGION.cognitiveservices.azure.com/",
    "Region": "YOUR_REGION"
  },
  "Recognition": {
    "Language": "ja-JP"
  }
}
```

**⚠️ セキュリティ警告**: 
- `appsettings.Development.json`は`.gitignore`に含まれています
- `appsettings.json`は本番用のプレースホルダー値のみでGitにコミット可能です
- APIキーを含むファイルをコミットしないでください

### 3. アプリケーションの実行

```bash
dotnet build
dotnet run
```

ブラウザで `https://localhost:5001` を開きます。

## 使い方

1. **マイクON**ボタンをクリックして録音開始
2. 音声が自動的にリアルタイムで文字起こしされます(左側)
3. **速記者メモ**欄にメモを入力してEnterで追加
4. **停止ボタン**をクリックすると話者分離が実行されます(右側)
5. WebM録音ファイルはダウンロードフォルダに自動保存されます

## 技術スタック

- **バックエンド**: .NET 8.0 Blazor Server
- **Azure Speech SDK**: 1.47.0
- **Fast Transcription API**: 2024-11-15
- **フロントエンド**: Bootstrap 5.3.0
- **音声処理**: Web Audio API (ScriptProcessorNode)

## プロジェクト構造

```
├── Components/
│   └── Pages/
│       └── Home.razor          # メインUI
├── Services/
│   └── SpeechRecognitionService.cs  # Azure Speech統合
├── wwwroot/
│   └── js/
│       └── audioRecorder.js    # ブラウザ音声キャプチャ
├── appsettings.example.json    # 設定ファイルのテンプレート
└── Program.cs                  # アプリケーションエントリポイント
```

## セキュリティ上の注意

- APIキーは環境変数または安全なシークレット管理サービスに保存してください
- `appsettings.json`をGitにコミットしないでください
- 本番環境では適切な認証・認可を実装してください
- HTTPS接続を使用してください

## ライセンス

このプロジェクトはデモンストレーション目的で作成されています。

## トラブルシューティング

### マイクアクセスエラー
- ブラウザのマイク許可設定を確認してください
- HTTPSで接続していることを確認してください

### 音声認識が動作しない
- Azure Speech Serviceのリージョンが正しいか確認してください
- APIキーが有効か確認してください
- ブラウザコンソールでエラーを確認してください

### 話者分離が実行されない
- WebMファイルがダウンロードフォルダに保存されているか確認してください
- Fast Transcription APIがリージョンでサポートされているか確認してください

## 参考資料

- [Azure Speech Service Documentation](https://learn.microsoft.com/azure/cognitive-services/speech-service/)
- [Fast Transcription API](https://learn.microsoft.com/azure/cognitive-services/speech-service/fast-transcription-create)
- [Conversation Transcription](https://learn.microsoft.com/azure/cognitive-services/speech-service/conversation-transcription)
