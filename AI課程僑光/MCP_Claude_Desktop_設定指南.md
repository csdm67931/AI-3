# Claude Desktop × Obsidian × MCP 設定指南

> **這份文件是給課後想動手實做的你**
>
> 課堂上看到 Claude 直接讀我的 Obsidian vault，那個「魔法」其實是 **MCP (Model Context Protocol)** 在背後串起來的。
> 跟著這份指南做，你也能讓 Claude Desktop 直接連上你的 vault。
>
> 講師｜廖梓棋（LINE: csdm67931）
> 適用：Windows / macOS（Linux 步驟雷同，自行對照）

---

## 0. 你會得到什麼？

設定完之後，你可以這樣跟 Claude 對話：

> 「幫我看看 vault 裡 `Daily/` 資料夾這週的筆記，整理一份本週學習摘要。」
>
> 「我在寫一篇關於 X 的文章，從 vault 找出所有相關筆記，列出可引用的段落。」
>
> 「把剛剛這段對話的結論，寫成 markdown 存到 vault 的 `Inbox/` 資料夾。」

Claude **直接讀、直接寫**你電腦上的 Obsidian vault——資料完全留在本機，沒有上傳雲端。

---

## 1. 你需要先準備什麼？

### ✅ 必備

| 項目 | 說明 | 下載 |
|---|---|---|
| Claude Desktop | Anthropic 官方桌面程式 | [claude.ai/download](https://claude.ai/download) |
| Obsidian | 本地 Markdown 筆記工具 | [obsidian.md](https://obsidian.md) |
| Node.js (LTS) | 跑 MCP server 用 | [nodejs.org](https://nodejs.org) |

### ✅ 一個 Obsidian Vault

如果你還沒建——
1. 打開 Obsidian → `Create new vault`
2. 取一個你記得的名字，例如 `MyBrain`
3. 選一個位置（建議：`C:\Users\你的名字\Documents\MyBrain` 或 `~/Documents/MyBrain`）
4. 進去隨便寫幾篇 .md 測試

---

## 2. 兩條設定路徑：選一條

### 🟢 路徑 A：Desktop Extensions（推薦，最簡單）

Claude Desktop 2025 之後內建「擴充功能」面板，可以一鍵安裝官方認證的 MCP server。

**步驟：**

1. 打開 Claude Desktop
2. 左下角點你的頭像 → **Settings** → **Extensions**（或 **Developer** → **Extensions**）
3. 找到 **Filesystem**（檔案系統）→ 點 **Install**
4. 設定面板會跳出，請它指向你的 vault 資料夾路徑
   - Windows: `C:\Users\你的名字\Documents\MyBrain`
   - macOS: `/Users/你的名字/Documents/MyBrain`
5. 儲存 → 重啟 Claude Desktop

**驗證：**
打開新對話，問：「請列出我 vault 根目錄的檔案。」
如果有列出檔名，就成功了。

---

### 🟡 路徑 B：手動編輯 config（傳統做法、跨版本通用）

如果你的 Claude Desktop 還沒有 Extensions 面板，或你想用更進階的設定，走這條。

#### Step 1：找到 config 檔位置

**Windows：**
按 `Win + R` → 貼上：
```
%APPDATA%\Claude
```
按 Enter，會跳出 Claude 的設定資料夾。裡面找 `claude_desktop_config.json`（沒有就自己新增）。

**macOS：**
打開 Finder → `Cmd + Shift + G` → 貼上：
```
~/Library/Application Support/Claude
```

#### Step 2：編輯 config

用任何文字編輯器打開 `claude_desktop_config.json`（記事本、VS Code 都可以），貼上以下內容：

**Windows 版本：**
```json
{
  "mcpServers": {
    "filesystem": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-filesystem",
        "C:\\Users\\你的名字\\Documents\\MyBrain"
      ]
    }
  }
}
```

**macOS 版本：**
```json
{
  "mcpServers": {
    "filesystem": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-filesystem",
        "/Users/你的名字/Documents/MyBrain"
      ]
    }
  }
}
```

⚠️ **重點：**
- Windows 路徑要用 `\\`（兩個反斜線）
- 把「你的名字」換成你的使用者帳號名稱
- 把 `MyBrain` 換成你 vault 的實際名稱

#### Step 3：存檔 → 完全關閉 Claude Desktop → 重新打開

不是按右上角 X，是右鍵工作列圖示 → Quit / 結束。

#### Step 4：驗證

新開對話，問：「請列出我 vault 根目錄的檔案。」
如果 Claude 顯示「需要使用 filesystem 工具」並列出檔案，成功。

---

## 3. 進階：同時連多個資料夾

如果你想讓 Claude 不只讀 vault，還能讀「下載」、「桌面」等資料夾——把 `args` 改成多個路徑：

```json
{
  "mcpServers": {
    "filesystem": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-filesystem",
        "C:\\Users\\你的名字\\Documents\\MyBrain",
        "C:\\Users\\你的名字\\Downloads",
        "C:\\Users\\你的名字\\Desktop"
      ]
    }
  }
}
```

Claude 只能讀你允許的這幾個資料夾，其他完全碰不到。

---

## 4. 設定好之後，5 個立刻可用的 Prompt

複製貼上即用，全部都會直接讀你的 vault：

### Prompt 1：今日整理
```
請讀我 vault 裡 Daily/ 資料夾今天日期的筆記，
幫我整理出三件事：今天學到什麼、做了什麼、想到什麼。
寫成一份摘要，附上「跟舊筆記的可能連結」。
```

### Prompt 2：知識串聯
```
我正在寫關於「[主題]」的內容。
請從我 vault 裡找出所有相關的 .md 檔，
列出哪幾篇可以引用、引用的具體段落是什麼。
```

### Prompt 3：書籍解構
```
我剛讀完一本書，書名是「[書名]」，重點是 [貼上你的筆記]。
幫我：
1. 拆解成 5–7 個獨立的概念 note
2. 每個 note 標一個 markdown 標籤
3. 列出這些概念可能跟我 vault 裡哪些舊筆記有關
4. 把結果存進 vault 的 Books/[書名]/ 資料夾
```

### Prompt 4：HANDOFF 交接筆記
```
我們今天的對話接近尾聲了。
請幫我寫一份 HANDOFF 筆記，存到我 vault 的 Handoffs/ 資料夾，
檔名格式：HANDOFF_YYYY-MM-DD_HHMM_主題.md
內容包含：
- 我們討論了什麼
- 結論是什麼
- 下次接著做什麼
- 相關連結（用 [[雙向連結]] 格式）
```

### Prompt 5：每週回顧
```
請讀我 vault 裡 Daily/ 資料夾過去 7 天的筆記，
整理出：
1. 我這週重複出現的主題
2. 我這週的最大進展
3. 我這週沒完成、要延續到下週的事
4. 一個可以放進 vault 「灼見」資料夾的洞察
```

---

## 5. 常見問題排錯

### Q1：Claude 說「我沒有檔案存取工具」
- ✅ 確認 config 檔位置正確
- ✅ 確認 JSON 格式沒有語法錯誤（少逗號、引號等）→ 可用 [jsonlint.com](https://jsonlint.com) 檢查
- ✅ 確認 Claude Desktop **完全重啟**（右鍵工作列 → Quit）

### Q2：說「找不到指定路徑」
- ✅ 路徑用兩個反斜線 `\\`（Windows）
- ✅ 確認資料夾真的存在
- ✅ 路徑裡有中文/空格？建議用引號包起來，或把 vault 移到無中文路徑

### Q3：說「npx command not found」
- ✅ Node.js 沒裝好——重新安裝 LTS 版本
- ✅ 安裝後重開機（讓 PATH 環境變數生效）

### Q4：可以連 Notion / Google Drive 嗎？
- ✅ 可以，有對應的 MCP server
- ✅ 但本指南聚焦在「本地 vault」這條最純粹、最安全的路徑
- ✅ 進階：搜尋 `@modelcontextprotocol/server-*` 看可用清單

---

## 6. 安全提醒（請務必看）

| 注意 | 說明 |
|---|---|
| 🔒 **只給 Claude 讀寫該讀寫的資料夾** | 不要把整個 C:\ 或 / 給它 |
| 🔒 **敏感資料另存加密 vault** | 客戶資料、密碼、財務文件不要跟一般筆記混 |
| 🔒 **定期備份** | Obsidian 純檔案，請設 Git 或雲端同步備份 |
| 🔒 **離線優先** | MCP 是本機協議，但對話本身仍會傳給 Anthropic。敏感對話可改用 [Claude 本地模型] 替代方案 |

---

## 7. 下一步：讓你的 vault 更強

設定完 MCP，接下來這幾個 Plugin 會讓 Obsidian × Claude 的搭配更威：

| Plugin | 用途 | 為什麼裝 |
|---|---|---|
| **Templater** | 自動化模板 | Daily note 一鍵生成、HANDOFF 模板自動填日期 |
| **Dataview** | 把 vault 變資料庫 | 用查詢語句撈出符合條件的筆記，給 Claude 當素材 |
| **Periodic Notes** | 週/月/年回顧 | 自動建立每週、每月回顧筆記 |
| **Smart Connections** | 用 AI 找相關筆記 | 寫 note 時側欄自動顯示相關舊筆記 |
| **Calendar** | 視覺化 Daily | 看月曆點日期跳到該日 note |

---

## 8. 我自己怎麼用這套？

- **每天**：開 Claude Desktop，問「整理我今天的 daily note」
- **接案前**：問「找我 vault 裡所有跟 [客戶領域] 有關的筆記」
- **學新東西**：把資料丟進 Inbox/，請 Claude 整理進對應 MOC
- **每週日**：跑「每週回顧」Prompt，把這週的成長封存

**重點不是工具，是你願不願意持續餵它素材。**
工具設好之後，剩下的就是你的選擇——
**今天起，每一次跟 AI 的對話，都不再蒸發。**

---

## 9. 卡關了？來找我

設定過程遇到問題、跑出奇怪錯誤、或是想交流自己的 vault 架構——

📱 **LINE ID：csdm67931**（廖梓棋）

歡迎拍照截圖丟給我，我看到會回。

---

> 「AI 不會取代你。但 AI 會取代不會用 AI 累積自己的人。」
> 願你的第二大腦長得又快又茂盛。
