using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Light2D;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;

/// <summary>
/// Generic grenade base.
/// </summary>
[RequireComponent(typeof(Pickupable))]
public class FireLemon : NetworkBehaviour, IPredictedInteractable<HandActivate>, IServerDespawn
{
	[Tooltip("Lowest potency explosion")]
	public Explosion explosionPrefab1;

	[Tooltip("Second lowest potency explosion")]
	public Explosion explosionPrefab2;

	[Tooltip("Medium potency explosion")]
	public Explosion explosionPrefab3;

	[Tooltip("Second highest potency explosion")]
	public Explosion explosionPrefab4;

	[Tooltip("Highest potency explosion")]
	public Explosion explosionPrefab5;

	[TooltipAttribute("If the fuse is precise or has a degree of error equal to fuselength / 4")]
	public bool unstableFuse = false;
	[TooltipAttribute("fuse timer in seconds")]
	public float fuseLength = 3;

	[Tooltip("SpriteHandler used for blinking animation")]
	public SpriteHandler spriteHandler;
	[Tooltip("Used for inventory animation")]
	public Pickupable pickupable;

	// Zero and one sprites reserved for left and right hands
	private const int LOCKED_SPRITE = 2;
	private const int ARMED_SPRITE = 3;

	private GrownFood GrownFood;

	private int lemonPotency;

	///Getting Grownfood so we can get the potency of the plant.
	private void Awake()
	{
		GrownFood = GetComponent<GrownFood>();
		lemonPotency = GrownFood.plantpotency;
	}

	//whether this object has exploded
	private bool hasExploded;

	// is timer finished or was interupted?
	[SyncVar(hook = nameof(UpdateTimer))]
	private bool timerRunning = false;

	//this object's registerObject
	private RegisterItem registerItem;
	private ObjectBehaviour objectBehaviour;

	private void Start()
	{
		registerItem = GetComponent<RegisterItem>();
		objectBehaviour = GetComponent<ObjectBehaviour>();

		// Set grenade to locked state by default
		UpdateSprite(LOCKED_SPRITE);
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		// Set grenade to locked state by default
		UpdateSprite(LOCKED_SPRITE);
		// Reset grenade timer
		timerRunning = false;
		hasExploded = false;
	}

	public void ClientPredictInteraction(HandActivate interaction)
	{
		// Toggle the throw action after activation
		UIManager.Action.Throw();
	}

	public void ServerRollbackClient(HandActivate interaction)
	{
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		if (timerRunning)
			return;

		// Toggle the throw action after activation
		if (interaction.Performer == PlayerManager.LocalPlayer)
		{
			UIManager.Action.Throw();
		}

		// Start timer
		StartCoroutine(TimeExplode(interaction.Performer));
	}

	private IEnumerator TimeExplode(GameObject originator)
	{
		if (!timerRunning)
		{
			timerRunning = true;
			PlayPinSFX(originator.transform.position);

			if (unstableFuse)
			{
				float fuseVariation = fuseLength / 4;
				fuseLength = Random.Range(fuseLength - fuseVariation, fuseLength + fuseVariation);
			}

			yield return WaitFor.Seconds(fuseLength);

			// Is timer still running?
			if (timerRunning)
				Explode();
		}
	}

	private void UpdateSprite(int sprite)
	{
		// Update sprite in game
		spriteHandler?.ChangeSprite(sprite);
	}

	/// <summary>
	/// This coroutines make sure that sprite in hands is animated
	/// TODO: replace this with more general aproach for animated icons
	/// </summary>
	/// <returns></returns>
	private IEnumerator AnimateSpriteInHands()
	{
		while (timerRunning && !hasExploded)
		{
			pickupable.RefreshUISlotImage();
			yield return null;
		}
	}

	public void Explode()
	{
		if (hasExploded)
		{
			return;
		}
		hasExploded = true;

		if (isServer)
		{
			// Get data from grenade before despawning
			var explosionMatrix = registerItem.Matrix;
			var worldPos = objectBehaviour.AssumedWorldPositionServer();

			// Despawn grenade
			Despawn.ServerSingle(gameObject);

			// Explosion here
			if(lemonPotency < 30)
			{
				var explosionGO = Instantiate(explosionPrefab1, explosionMatrix.transform);
				explosionGO.transform.position = worldPos;
				explosionGO.Explode(explosionMatrix);
			}

			if(lemonPotency >= 31 && lemonPotency <= 50)
			{
				var explosionGO = Instantiate(explosionPrefab2, explosionMatrix.transform);
				explosionGO.transform.position = worldPos;
				explosionGO.Explode(explosionMatrix);
			}

			if(lemonPotency >= 51 && lemonPotency <= 70)
			{
				var explosionGO = Instantiate(explosionPrefab3, explosionMatrix.transform);
				explosionGO.transform.position = worldPos;
				explosionGO.Explode(explosionMatrix);
			}

			if(lemonPotency >= 71 && lemonPotency <= 90)
			{
				var explosionGO = Instantiate(explosionPrefab4, explosionMatrix.transform);
				explosionGO.transform.position = worldPos;
				explosionGO.Explode(explosionMatrix);
			}

			if(lemonPotency > 90)
			{
				var explosionGO = Instantiate(explosionPrefab5, explosionMatrix.transform);
				explosionGO.transform.position = worldPos;
				explosionGO.Explode(explosionMatrix);
			}
		}
	}

	private void PlayPinSFX(Vector3 position)
	{
		SoundManager.PlayNetworkedAtPos("armbomb", position, sourceObj: gameObject);
	}

	private void UpdateTimer(bool wasTimerRunning, bool timerRunning)
	{
		this.timerRunning = timerRunning;

		if (timerRunning)
		{
			// Start playing arm animation
			UpdateSprite(ARMED_SPRITE);
			// Update grenade icon in hands
			StartCoroutine(AnimateSpriteInHands());
		}
		else
		{
			// We somehow deactivated bomb
			UpdateSprite(LOCKED_SPRITE);
		}

	}

#if UNITY_EDITOR
	/// <summary>
	/// Used only for debug in editor
	/// </summary>
	[ContextMenu("Pull a pin")]
	private void PullPin()
	{
		StartCoroutine(TimeExplode(gameObject));
	}
#endif
}