using Runtime.Script;
using Runtime.Script.Charts;
using System;
using System.Drawing;
using TradeApi.History;
using TradeApi.Indicators;

// ==========================================================
//  ATR SuperTrend + GOLD_SIGN 雙層整合指標（精簡版）
//
//  ┌─ Layer 1：GOLD_SIGN 結構方向（主控） ─────────────────┐
//  │  HH = 分段平均高點（結構阻力）                        │
//  │  LL = 分段平均低點（結構支撐）                        │
//  │  close > HH → 結構多頭 (+1)                          │
//  │  close < LL → 結構空頭 (-1)                          │
//  │  LL~HH 之間 → 維持前值（灰色地帶，無信號）            │
//  └──────────────────────────────────────────────────────┘
//       ↓ goldTrend 決定線段顏色與信號方向
//  ┌─ Layer 2：SuperTrend 動態帶（追蹤線段用） ────────────┐
//  │  多頭模式：下軌只升不降（支撐線不追跌）               │
//  │  空頭模式：上軌只降不升（阻力線不追漲）               │
//  └──────────────────────────────────────────────────────┘
// ==========================================================

namespace ATR_SuperTrend_ADX
{
    public class ATR_SuperTrend_ADX : IndicatorBuilder
    {
        #region 建構式
        public ATR_SuperTrend_ADX() : base()
        {
            #region Initialization
            Credentials.ProjectName = "SuperTrend + GOLD_SIGN 雙層整合";
            Credentials.Description = "GOLD_SIGN結構帶主控方向，SuperTrend追蹤動態支撐阻力線";
            Credentials.Author      = "Bob";
            Credentials.DateOfCreation = new DateTime(2026, 4, 16);
            #endregion

            // ── Layer 1：GOLD_SIGN 結構帶（虛線參考）────────────────
            Lines.Set("StructHigh");
            Lines["StructHigh"].Color = Color.Tomato;
            Lines["StructHigh"].Width = 1;
            Lines["StructHigh"].Style = LineStyle.Dot;

            Lines.Set("StructLow");
            Lines["StructLow"].Color = Color.DodgerBlue;
            Lines["StructLow"].Width = 1;
            Lines["StructLow"].Style = LineStyle.Dot;

            // ── Layer 2：SuperTrend 動態帶（實線顯示）───────────────
            Lines.Set("Support");
            Lines["Support"].Color = Color.Lime;
            Lines["Support"].Width = 2;

            Lines.Set("Resistance");
            Lines["Resistance"].Color = Color.OrangeRed;
            Lines["Resistance"].Width = 2;

            // ── 翻轉信號箭頭 ─────────────────────────────────────────
            Lines.Set("SignUp");
            Lines["SignUp"].Style     = LineStyle.Symbol;
            Lines["SignUp"].ArrowCode = 0233;
            Lines["SignUp"].Color     = Color.Red;
            Lines["SignUp"].Width     = 5;

            Lines.Set("SignDown");
            Lines["SignDown"].Style     = LineStyle.Symbol;
            Lines["SignDown"].ArrowCode = 0234;
            Lines["SignDown"].Color     = Color.Lime;
            Lines["SignDown"].Width     = 5;

            // ── 加碼信號箭頭 (Pyramid) ──────────────────────────────────
            Lines.Set("PyramidUp");
            Lines["PyramidUp"].Style     = LineStyle.Symbol;
            Lines["PyramidUp"].ArrowCode = 0233;
            Lines["PyramidUp"].Color     = Color.Yellow;
            Lines["PyramidUp"].Width     = 4;

            Lines.Set("PyramidDown");
            Lines["PyramidDown"].Style     = LineStyle.Symbol;
            Lines["PyramidDown"].ArrowCode = 0234;
            Lines["PyramidDown"].Color     = Color.White;
            Lines["PyramidDown"].Width     = 4;

            SeparateWindow = false;
        }
        #endregion

        #region 輸入參數

        // ── Layer 1：GOLD_SIGN 結構帶 ──────────────────────────────
        [InputParameter(InputType.Numeric, "結構週期（GOLD_SIGN 主週期）", 0)]
        [SimpleNumeric(1D, 99999D)]
        public int StructPeriod = 15;

        [InputParameter(InputType.Checkbox, "顯示結構帶（HH / LL 虛線）", 1)]
        public bool ShowStructuralBands = true;

        // ── Layer 2：SuperTrend 動態帶 ─────────────────────────────
        [InputParameter(InputType.Numeric, "ATR週期", 2)]
        [SimpleNumeric(1D, 99999D)]
        public int ATRPeriod = 14;

        [InputParameter(InputType.Numeric, "ATR倍數（通道寬度）", 3)]
        [SimpleNumeric(1D, 20D)]
        public double ATRMultiplier = 3.0;

        // ── 顯示選項 ──────────────────────────────────────────────
        [InputParameter(InputType.Checkbox, "顯示動態支撐阻力線", 4)]
        public bool ShowLines = true;

        [InputParameter(InputType.Checkbox, "顯示信號箭頭", 5)]
        public bool ShowArrows = true;

        [InputParameter(InputType.Numeric, "反轉 ATR 緩衝倍數 (Buffer)", 5)]
        [SimpleNumeric(0D, 5D)]
        public double BufferMultiplier = 2.0;

        [InputParameter(InputType.Numeric, "RSI 週期", 6)]
        [SimpleNumeric(2D, 100D)]
        public int RSIPeriod = 14;

        [InputParameter(InputType.Numeric, "多頭拉回 RSI 解鎖值 (通常 50)", 7)]
        [SimpleNumeric(10D, 90D)]
        public int RSIPullbackLong = 50;

        [InputParameter(InputType.Numeric, "空頭反彈 RSI 解鎖值 (通常 50)", 8)]
        [SimpleNumeric(10D, 90D)]
        public int RSIPullbackShort = 50;

        [InputParameter(InputType.Numeric, "EMA 快線週期", 9)]
        [SimpleNumeric(10D, 300D)]
        public int EMAPeriod = 60;

        [InputParameter(InputType.Numeric, "加碼信號最小間隔 (Bars)", 10)]
        [SimpleNumeric(1D, 100D)]
        public int PyramidInterval = 15;

        #endregion

        #region 內部變數

        // Layer 1：GOLD_SIGN
        private int    barsPerPeriod = 10;  // 每結構週期的K棒數（時間週期自適應）

        // Layer 2：SuperTrend & Dynamics
        private BuiltInIndicator atr;
        private BuiltInIndicator rsi;
        private BuiltInIndicator ema;


        private int  minBarsRequired;
        private bool historyCalculated = false;

        // 加碼狀態追蹤
        private int  activeSignalTrend = 0;      // 確保加碼方向與有效主趨勢一致
        private int  lastSignalBar = -999; 
        private int  currentBarCount = 0;
        private int  pyramidCount = 0;           // 趨勢內加碼次數限制 (Max 3)
        private bool pullbackWaiting = false;    // 是否已進入回撤區域 (RSI < 30 for Buy)
        private bool prevPullbackWaiting = false; 

        // 記憶趨勢狀態以偵測翻轉
        private int    prevGoldTrend = 0;   // 1=結構多頭 / -1=結構空頭 / 0=未初始化
        private double prevUpperBand = 0.0;
        private double prevLowerBand = 0.0;

        // 箭頭偏移量（ATR 倍數）
        private const double ARROW_ATR_RATIO = 0.5;

        #endregion

        #region Init
        public override void Init()
        {
            // 依圖表時間週期換算每結構週期K棒數
            barsPerPeriod = GetBarsPerPeriod();

            // 最小K棒需求：GOLD_SIGN 結構帶 vs ATR 暖身期，取較大值
            int goldMinBars = barsPerPeriod * (StructPeriod + 2);
            int atrMinBars  = ATRPeriod * 2 + 10;
            minBarsRequired = Math.Max(goldMinBars, atrMinBars);

            // 建立 ATR 指標
            atr = IndicatorsManager.BuildIn.ATR(HistoryDataSeries, ATRPeriod);
            
            // 建立新指標工具 (RSI & EMA)
            rsi = IndicatorsManager.BuildIn.RSI(HistoryDataSeries, RSIPeriod);
            ema = IndicatorsManager.BuildIn.MA(HistoryDataSeries, EMAPeriod, MAMode.EMA);

            ScriptShortName = string.Format("PULLBACK_v2({0}) RSI({1})", StructPeriod, RSIPeriod);

            // 重置狀態
            historyCalculated = false;
            prevGoldTrend       = 0;
            prevUpperBand       = 0.0;
            prevLowerBand       = 0.0;
            activeSignalTrend   = 0;
            lastSignalBar       = -999;
            currentBarCount     = 0;
            pyramidCount        = 0;
            pullbackWaiting     = false;
            prevPullbackWaiting = false;
        }
        #endregion

        #region Update
        public override void Update(TickStatus args)
        {
            if (HistoryDataSeries.Count < minBarsRequired)
                return;

            if (!historyCalculated)
            {
                CalculateHistory();
                historyCalculated = true;
                currentBarCount = HistoryDataSeries.Count;
            }

            // --- 狀態持久化修復 ---
            // 當 K 棒總數增加，代表上一根 K 棒已封裝。此時必須執行一次 CalculateBar(1) 
            // 讓 prevGoldTrend 等變數「繼承」上一根 K 棒結束時的最準確結果。
            if (HistoryDataSeries.Count > currentBarCount)
            {
                CalculateBar(1); 
                currentBarCount = HistoryDataSeries.Count;
                // 提交狀態
                prevPullbackWaiting = pullbackWaiting;
            }

            CalculateBar(0);
        }
        #endregion

        #region 歷史回補
        private void CalculateHistory()
        {
            prevGoldTrend = 0;
            prevUpperBand = 0.0;
            prevLowerBand = 0.0;
            activeSignalTrend = 0;

            int startBar = HistoryDataSeries.Count - minBarsRequired;
            if (startBar < 0) startBar = 0;

            for (int bar = startBar; bar >= 1; bar--)
                CalculateBar(bar);
        }
        #endregion

        #region 單根K棒計算
        private void CalculateBar(int bar)
        {
            // ════════════════════════════════════════════════════
            // STEP 0：獲取基礎數據
            // ════════════════════════════════════════════════════
            double HH = AverageHigh_Simple(bar, StructPeriod);
            double LL = AverageLow_Simple(bar, StructPeriod);
            if (HH <= 0 || LL <= 0 || HH <= LL) return;

            if (ShowStructuralBands)
            {
                Lines["StructHigh"].SetValue(HH, bar);
                Lines["StructLow"].SetValue(LL,  bar);
            }

            double close    = HistoryDataSeries.GetValue(PriceType.Close, bar);
            double high     = HistoryDataSeries.GetValue(PriceType.High,  bar);
            double low      = HistoryDataSeries.GetValue(PriceType.Low,   bar);
            double atrValue = atr.GetValue(bar);
            if (atrValue <= 0) return;

            // ════════════════════════════════════════════════════
            // STEP 1：帶緩衝的趨勢判定 (ATR Buffer + ADX Gate)
            // ════════════════════════════════════════════════════
            double upperLimit = HH + BufferMultiplier * atrValue;
            double lowerLimit = LL - BufferMultiplier * atrValue;

            int goldTrend = prevGoldTrend;
            bool isReversal = false;

            // 結構突破判定
            if (close > upperLimit)
            {
                if (goldTrend != 1) {
                    goldTrend = 1;
                    isReversal = true;
                }
            }
            else if (close < lowerLimit)
            {
                if (goldTrend != -1) {
                    goldTrend = -1;
                    isReversal = true;
                }
            }

            // ════════════════════════════════════════════════════
            // STEP 2：SuperTrend 動態軌道處理 (支撐不跌/阻力不漲)
            // ════════════════════════════════════════════════════
            double mid        = (high + low) / 2.0;
            double basicUpper = mid + ATRMultiplier * atrValue;
            double basicLower = mid - ATRMultiplier * atrValue;
            double finalUpper, finalLower;

            if (goldTrend >= 0) {
                finalLower = (prevLowerBand <= 0.0 || basicLower > prevLowerBand) ? basicLower : prevLowerBand;
                finalUpper = basicUpper;
            } else {
                finalUpper = (prevUpperBand <= 0.0 || basicUpper < prevUpperBand) ? basicUpper : prevUpperBand;
                finalLower = basicLower;
            }

            if (ShowLines) {
                if (goldTrend == 1) Lines["Support"].SetValue(finalLower, bar);
                else if (goldTrend == -1) Lines["Resistance"].SetValue(finalUpper, bar);
            }

            // ════════════════════════════════════════════════════
            // STEP 3：信號產出 (RSI 拉回加碼版 - 限 3 次)
            // ════════════════════════════════════════════════════
            if (ShowArrows)
            {
                // 1. 取得指標數值 (當前與上一根，用於穿越判斷)
                double rsiCurr = rsi.GetValue(bar);
                double rsiPrev = rsi.GetValue(bar + 1);
                double emaVal  = ema.GetValue(bar);
                
                // 計算絕對 Bar 索引
                int absoluteBarIndex = HistoryDataSeries.Count - bar;
                bool isSpaced = (absoluteBarIndex - lastSignalBar) >= PyramidInterval;

                // A. 翻轉訊號 (Reversal)
                if (isReversal)
                {
                    pyramidCount = 0;         // 趨勢只要翻轉，無條件重置加碼計數
                    pullbackWaiting = false;  // 重置拉回狀態

                    if (isSpaced)
                    {
                        if (goldTrend == 1)
                            Lines["SignUp"].SetValue(low - ARROW_ATR_RATIO * atrValue, bar);
                        else
                            Lines["SignDown"].SetValue(high + ARROW_ATR_RATIO * atrValue, bar);

                        lastSignalBar = absoluteBarIndex;
                        activeSignalTrend = goldTrend; // 綁定有效的趨勢信號
                    }
                    else
                    {
                        activeSignalTrend = 0; // 主信號被過濾，則不允許這波發送加碼單
                    }
                }
                // B. 加碼訊號 (Pullback Scale-in) - 當趨勢延續、同向主信號有效，且次數 < 3
                else if (goldTrend != 0 && goldTrend == prevGoldTrend && goldTrend == activeSignalTrend && pyramidCount < 3)
                {
                    // 1. 偵測拉回 (Pullback Trigger)
                    if (goldTrend == 1 && rsiCurr < RSIPullbackLong) pullbackWaiting = true;
                    else if (goldTrend == -1 && rsiCurr > RSIPullbackShort) pullbackWaiting = true;

                    // 2. 偵測再爆發 (Re-breakout Condition)
                    bool isTrigger = false;
                    if (pullbackWaiting && isSpaced)
                    {
                        // 多頭：RSI 向上突破設定值且價格在 EMA 之上
                        if (goldTrend == 1 && close > emaVal && rsiPrev < RSIPullbackLong && rsiCurr >= RSIPullbackLong)
                        {
                            Lines["PyramidUp"].SetValue(low - ARROW_ATR_RATIO * atrValue, bar);
                            isTrigger = true;
                        }
                        // 空頭：RSI 向下突破設定值且價格在 EMA 之下
                        else if (goldTrend == -1 && close < emaVal && rsiPrev > RSIPullbackShort && rsiCurr <= RSIPullbackShort)
                        {
                            Lines["PyramidDown"].SetValue(high + ARROW_ATR_RATIO * atrValue, bar);
                            isTrigger = true;
                        }
                    }

                    if (isTrigger)
                    {
                        pyramidCount++;          // 增加加碼計數
                        lastSignalBar = absoluteBarIndex;
                        pullbackWaiting = false; // 加碼後重置拉回狀態
                    }
                }
            }

            // 更新歷史狀態
            if (bar > 0)
            {
                prevGoldTrend = goldTrend;
                prevUpperBand = finalUpper;
                prevLowerBand = finalLower;
            }
        }
        #endregion

        #region GOLD_SIGN 輔助函數

        /// <summary>依圖表時間週期自動換算每結構週期的K棒數</summary>
        private int GetBarsPerPeriod()
        {
            if (HistoryDataSeries.HistoricalRequest is TimeHistoricalRequest timeReq)
            {
                Period period = timeReq.Period;
                int    value  = timeReq.Value;

                switch (period)
                {
                    case Period.Minute:
                        if (value == 1)  return 60;
                        if (value == 2)  return 30;
                        if (value == 3)  return 20;
                        if (value == 4)  return 15;
                        if (value == 5)  return 12;
                        if (value == 6)  return 10;
                        if (value == 10) return 6;
                        if (value == 12) return 5;
                        if (value == 15) return 4;
                        if (value == 20) return 3;
                        if (value == 30) return 8;
                        return Math.Max(1, 60 / value);

                    case Period.Hour:
                        if (value == 1)  return 4;
                        if (value == 2)  return 2;
                        if (value == 3)  return 2;
                        if (value == 4)  return 6;
                        if (value == 6)  return 4;
                        if (value == 8)  return 3;
                        if (value == 12) return 2;
                        return Math.Max(1, 24 / value);

                    case Period.Day:   return 5;
                    case Period.Week:  return 4;
                    case Period.Month: return 12;
                    default:           return 10;
                }
            }
            return 10;
        }

        /// <summary>分段平均高點：將回溯區間切成 period 段，每段取最高，最後平均</summary>
        private double AverageHigh_Simple(int index, int period)
        {
            int lookback = period * barsPerPeriod;
            if (index + lookback >= HistoryDataSeries.Count)
                return 0.0;

            double sum = 0.0;
            for (int i = 0; i < period; i++)
            {
                int    startPos    = index + i * barsPerPeriod;
                double segmentHigh = double.MinValue;

                for (int j = 0; j < barsPerPeriod; j++)
                {
                    int pos = startPos + j;
                    if (pos >= HistoryDataSeries.Count) return 0.0;
                    double h = HistoryDataSeries.GetValue(PriceType.High, pos);
                    if (h > segmentHigh) segmentHigh = h;
                }

                if (segmentHigh == double.MinValue) return 0.0;
                sum += segmentHigh;
            }
            return sum / period;
        }

        /// <summary>分段平均低點：將回溯區間切成 period 段，每段取最低，最後平均</summary>
        private double AverageLow_Simple(int index, int period)
        {
            int lookback = period * barsPerPeriod;
            if (index + lookback >= HistoryDataSeries.Count)
                return 0.0;

            double sum = 0.0;
            for (int i = 0; i < period; i++)
            {
                int    startPos   = index + i * barsPerPeriod;
                double segmentLow = double.MaxValue;

                for (int j = 0; j < barsPerPeriod; j++)
                {
                    int pos = startPos + j;
                    if (pos >= HistoryDataSeries.Count) return 0.0;
                    double l = HistoryDataSeries.GetValue(PriceType.Low, pos);
                    if (l < segmentLow) segmentLow = l;
                }

                if (segmentLow == double.MaxValue) return 0.0;
                sum += segmentLow;
            }
            return sum / period;
        }

        #endregion

        #region 繪製狀態面板
        public override void OnPaintChart(object sender, PaintChartEventArgs args)
        {
            base.OnPaintChart(sender, args);

            // 確保有足夠的 K 棒資料與 Graphics 物件
            if (args.Graphics == null || HistoryDataSeries.Count < 1) return;

            // 取得畫布
            Graphics g = args.Graphics;

            // 設定面板位置與大小
            int panelX = 10;
            int panelY = 40;  // 避免與系統預設的左上角文字重疊
            int panelWidth = 260;
            int panelHeight = 160;

            // 顏色與字型設定
            Color bgColor = Color.FromArgb(200, 30, 30, 30);
            Brush bgBrush = new SolidBrush(bgColor);
            Font titleFont = new Font("Microsoft JhengHei UI", 10, FontStyle.Bold);
            Font normalFont = new Font("Microsoft JhengHei UI", 9);
            Brush textBrush = new SolidBrush(Color.WhiteSmoke);
            Brush highlightBrush = new SolidBrush(Color.Gold);
            Pen borderPen = new Pen(Color.Gray, 1);

            // 繪製背景
            g.FillRectangle(bgBrush, panelX, panelY, panelWidth, panelHeight);
            g.DrawRectangle(borderPen, panelX, panelY, panelWidth, panelHeight);

            // 解析目前狀態
            string trendStr = "震盪 (Wait)";
            Color trendColor = Color.LightGray;
            if (prevGoldTrend == 1) { trendStr = "多方 (Bullish)"; trendColor = Color.Lime; }
            else if (prevGoldTrend == -1) { trendStr = "空方 (Bearish)"; trendColor = Color.Tomato; }

            string activeSignalStr = "未發出或已過濾";
            Color activeColor = Color.LightGray;
            if (activeSignalTrend == 1) { activeSignalStr = "多方主單有效 (允許加碼)"; activeColor = Color.Lime; }
            else if (activeSignalTrend == -1) { activeSignalStr = "空方主單有效 (允許加碼)"; activeColor = Color.Tomato; }

            double currentRSI = rsi != null ? rsi.GetValue(0) : 0;
            
            // 繪製文字
            int yOffset = panelY + 10;
            g.DrawString("🔔 加碼與趨勢狀態監控面板", titleFont, highlightBrush, panelX + 10, yOffset);
            
            yOffset += 25;
            Brush tBrush = new SolidBrush(trendColor);
            g.DrawString($"目前結構趨勢: {trendStr}", normalFont, tBrush, panelX + 10, yOffset);
            tBrush.Dispose();

            yOffset += 20;
            Brush aBrush = new SolidBrush(activeColor);
            g.DrawString($"主訊號狀態: {activeSignalStr}", normalFont, aBrush, panelX + 10, yOffset);
            aBrush.Dispose();

            yOffset += 20;
            g.DrawString($"趨勢內已加碼次數: {pyramidCount} / 3", normalFont, textBrush, panelX + 10, yOffset);

            yOffset += 20;
            string pullbackStr = pullbackWaiting ? "🟡 等待再突破確認" : "無";
            g.DrawString($"拉回準備狀態: {pullbackStr}", normalFont, (pullbackWaiting ? highlightBrush : textBrush), panelX + 10, yOffset);

            yOffset += 20;
            string rsiDesc = "";
            if (activeSignalTrend == 1 && currentRSI < RSIPullbackLong) rsiDesc = "(觸發拉回)";
            else if (activeSignalTrend == -1 && currentRSI > RSIPullbackShort) rsiDesc = "(觸發拉回)";
            g.DrawString($"最新 RSI ({RSIPeriod}): {currentRSI:F1}  {rsiDesc}", normalFont, textBrush, panelX + 10, yOffset);

            // 釋放資源
            bgBrush.Dispose();
            textBrush.Dispose();
            highlightBrush.Dispose();
            borderPen.Dispose();
            titleFont.Dispose();
            normalFont.Dispose();
        }
        #endregion
    }
}
