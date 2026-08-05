using UnityEngine;
using DG.Tweening;

public class UIGemScatterManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Gem Prefab (UI Image)")]
    public GameObject gemPrefab;     
    
    [Tooltip("Canvas or Panel to hold spawned gems")]
    public Transform canvasParent;    
    
    [Tooltip("Target UI (e.g., Gem icon on the corner)")]
    public RectTransform targetUI;    

    [Header("Settings")]
    [Tooltip("Number of gems to spawn")]
    public int gemCount = 12;        
    
    [Tooltip("Sorting order. Higher means it renders on top.")]
    public int orderInLayer = 10;

    [Header("Gem Physics Simulation (Scatter)")]
    [Tooltip("How high gems jump during explosion")]
    public CustomFloatRange explodeHeight = new CustomFloatRange { fixedValue = 0.5f, minValue = 0.3f, maxValue = 0.8f };
    
    [Tooltip("How wide gems scatter horizontally")]
    public CustomFloatRange scatterAreaWidth = new CustomFloatRange { fixedValue = 2f, minValue = 1f, maxValue = 3f };

    [Tooltip("How far BELOW the spawn point is the 'floor'")]
    public CustomFloatRange floorOffset = new CustomFloatRange { fixedValue = 0.3f, minValue = 0.1f, maxValue = 0.5f };

    [Tooltip("Bounce speed multiplier. Default is 1. Higher value means faster bounce.")]
    public CustomFloatRange bounceSpeed = new CustomFloatRange { fixedValue = 1f, minValue = 0.8f, maxValue = 1.5f };

    [Tooltip("Percentage of height retained after each bounce (0-1)")]
    [Range(0f, 0.9f)]
    public float bounceMultiplier = 0.5f;

    [Tooltip("Number of bounces before lying still")]
    public int numBounces = 2;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector2 startPos = transform.position;
            PlayGemScatterEffect(startPos);
        }
    }

    public void PlayGemScatterEffect(Vector2 startPosition)
    {
        for (int i = 0; i < gemCount; i++)
        {
            // 1. Spawn gem
            GameObject gem = Instantiate(gemPrefab, canvasParent);
            gem.transform.position = startPosition;

            // --- HANDLE ORDER IN LAYER ---
            Canvas gemCanvas = gem.GetComponent<Canvas>();
            if (gemCanvas == null) 
            {
                gemCanvas = gem.AddComponent<Canvas>();
            }
            gemCanvas.overrideSorting = true;
            gemCanvas.sortingOrder = orderInLayer;

            // --- GET ACTUAL VALUES FOR THIS SPECIFIC GEM ---
            float currentExplodeHeight = explodeHeight.GetValue();
            float currentScatterWidth = scatterAreaWidth.GetValue();
            float currentFloorOffset = floorOffset.GetValue();
            float currentBounceSpeed = bounceSpeed.GetValue();

            // --- CALCULATION (PHYSICS SIMULATION) ---
            float landX = startPosition.x + Random.Range(-currentScatterWidth / 2f, currentScatterWidth / 2f);
            float floorY = startPosition.y - currentFloorOffset;

            // --- CREATE DOTWEEN SEQUENCE ---
            Sequence seq = DOTween.Sequence();

            // PHASE 1: EXPLOSION 
            seq.Append(gem.transform.DOMoveX(landX, 0.4f).SetEase(Ease.Linear));
            seq.Join(gem.transform.DOMoveY(startPosition.y + currentExplodeHeight, 0.2f).SetEase(Ease.OutQuad));
            seq.Append(gem.transform.DOMoveY(floorY, 0.2f).SetEase(Ease.InQuad));

            // PHASE 2: BOUNCING 
            float currentBounceHeight = currentExplodeHeight * bounceMultiplier;
            
            for (int b = 0; b < numBounces; b++)
            {
                // Calculate base duration and divide by currentBounceSpeed (Higher speed = shorter duration)
                float baseDuration = 0.1f + (currentBounceHeight / Mathf.Max(0.001f, currentExplodeHeight)) * 0.1f;
                float bounceDuration = baseDuration / Mathf.Max(0.01f, currentBounceSpeed);

                seq.Append(gem.transform.DOMoveY(floorY + currentBounceHeight, bounceDuration).SetEase(Ease.OutQuad));
                seq.Append(gem.transform.DOMoveY(floorY, bounceDuration).SetEase(Ease.InQuad));

                currentBounceHeight *= bounceMultiplier;
            }

            // PHASE 3: LIE STILL 
            seq.AppendInterval(Random.Range(0.2f, 0.5f));

            // PHASE 4: FLY TO TARGET UI
            seq.Append(gem.transform.DOMove(targetUI.position, 0.6f).SetEase(Ease.InBack));

            // 3. On Complete
            seq.OnComplete(() =>
            {
                Destroy(gem); 
                
                targetUI.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 0.1f).OnComplete(() =>
                {
                    targetUI.DOScale(Vector3.one, 0.1f);
                });
            });
        }
    }
}

// --- CUSTOM CLASS FOR FIXED OR RANDOM RANGE VALUES ---
[System.Serializable]
public class CustomFloatRange
{
    [Tooltip("Check this to use Min/Max random range. Uncheck to use Fixed Value.")]
    public bool useRandom;
    
    public float fixedValue;
    public float minValue;
    public float maxValue;

    public float GetValue()
    {
        return useRandom ? Random.Range(minValue, maxValue) : fixedValue;
    }
}