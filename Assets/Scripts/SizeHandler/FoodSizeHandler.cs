using System;
using UnityEngine;

public class FoodSizeHandler : SizeHandler
{
   private void OnDestroy()
   {
      if (SpawnManager.Instance != null) // SA1: guard during scene unload
         SpawnManager.Instance.CleanFoodArray(this);
   }
}
