# 📊 CVD Divergence (ATAS Indicator)

**CVD Divergence** is a custom indicator for the **ATAS** platform that analyzes the cumulative delta and automatically detects divergences between price and delta.

The indicator plots divergence lines and highlights them in different colors depending on the direction (bullish or bearish).

---

<img src="https://github.com/Eipix/CVD-Divergence/blob/master/assets/divergences_btc_futures.png">

---

## ⚙️ Parameters

<img src="https://github.com/Eipix/CVD-Divergence/blob/master/assets/divergence_parameters.png">

---

| Parameter | Type | Description |
|-----------|------|-------------|
| **Max Bars Distance** | `int` | Upper limit for defining divergences (in bars). |
| **Min Bars Distance** | `int` | Filters out divergences whose distance is less than the specified value. |
| **Bar Shadow Percent From Body** | `decimal` | The minimum percentage of the candle's shadow relative to its body for the extremum to be considered. |
| **Bullish Color** | `Color` | The color of bullish divergences. |
| **Bearish Color** | `Color` | The color of bearish divergences. |

---

## 📌 Integration into Other Indicators

**CVD Divergence** can be used as a composite indicator for strategies and other indicators.  
It consists of three DataSeries:

1. **ValueDataSeries**: cumulative delta values  
2. **ObjectDataSeries**: bullish divergences built from lower extrema, storing a **Divergence** structure  
3. **ObjectDataSeries**: bearish divergences built from upper extrema, storing a **Divergence** structure  

Example of a strategy that uses **CVD Divergence**: 

This example can be found in the **CVD Divergence/Samples** folder.
