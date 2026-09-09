using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System;
public class UIBarMove
{

    public void UINumberTextSmoothReduce(int limitNum, int startNum, int endNum, Action<int> onUpdateCallback = null)
    {

        // 使用 DOTween 來平滑變化數字
        DOTween.To(() => startNum, x => startNum = x, endNum, 0.5f)   // 0.5 秒內平滑從 startNum 變到 endNum
               .OnUpdate(() =>
               {
                   onUpdateCallback?.Invoke(startNum);
               })
               .SetEase(Ease.Linear);
    }

    public void UIWidth_SmoothReduce(RectTransform rt, float targetWidth, float period, Ease ease, Action onCompleteCallback = null)
    {
        UIMove(rt, targetWidth, period, ease, onCompleteCallback);  // 呼叫修改後的方法
    }


    void UIMove(RectTransform rt, float targetWidth, float period, Ease ease, Action onCompleteCallback = null)
    {
        // 使用 DOTween 平滑過渡 sizeDelta 的寬度
        Vector2 targetSize = new Vector2(targetWidth, rt.sizeDelta.y); // 保持高度不變，只改變寬度

        rt.DOSizeDelta(targetSize, period)  // 對 sizeDelta 做動畫
          .SetEase(ease)                    // 設置緩動效果
          .OnComplete(() =>                 // 動畫結束後觸發
          {
              onCompleteCallback?.Invoke(); // 執行傳入的回呼函數（若有）
          });

        // 使用 DOTween 平滑過渡 fillAmount， 0.5 秒完成過渡 使用平滑的緩動效果
        // rt.GetComponent<Image>().DOFillAmount(moveAmount, period)
        // .SetEase(ease)
        // .OnComplete(() =>  // 動畫結束後觸發
        //      {
        //          onCompleteCallback?.Invoke();     // 執行傳入的回呼函數（若有）
        //      });


    }
}
