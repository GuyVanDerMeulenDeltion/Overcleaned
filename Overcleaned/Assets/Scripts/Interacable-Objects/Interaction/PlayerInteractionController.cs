﻿using UnityEngine;
using Photon.Pun;
using System;
using System.Threading.Tasks;

public class PlayerInteractionController : MonoBehaviourPunCallbacks
{
    private const float RAY_LENGTH = 1f;

    [Header("References & Parameters:")]
    [SerializeField]
    private GameObject hand;

    [SerializeField]
    private LayerMask interactableMask;

    [SerializeField]
    private Transform arrow_Selection_UX;

    [SerializeField]
    private Animator playerAnimator;

    [SerializeField]
    private Transform h2_Anchor;

    [Header("Debugging:")]
    public WieldableObject currentlyWielding;

    public InteractableObject currentSelected;

    public InteractableObject currentlyInteracting;

    #region ### Private Variables ###
    private readonly KeyCode interactKey = KeyCode.E;
    private readonly KeyCode dropWieldableKey = KeyCode.F;
    private readonly KeyCode useWieldableKey = KeyCode.Space;

    private Vector3 arrow_UX_Offset = new Vector3(0, 2, 0);
    private Vector3 boxcast_HalfExtends = new Vector3(0.4f, 0.75f, 0.07f);

    private bool forceDrop = false;

    private const int HAND_LAYER = 1;
    #endregion

    #region ### RPC Calls ###
    [PunRPC]
    private void Cast_ThrowObject(int objectID, bool hasForceDropped) 
    {
        const float THROW_FORCE_FORWARD = 10;
        const float THROW_FORCE_UP = 3;

        playerAnimator.SetLayerWeight(HAND_LAYER, 0);

        Vector3 throwVelocity = hasForceDropped ? Vector3.zero : (transform.forward * THROW_FORCE_FORWARD) + (transform.up * THROW_FORCE_UP);

        Transform thrownObject = NetworkManager.GetViewByID(objectID).transform;

        thrownObject.transform.SetParent(null);
        thrownObject.transform.GetComponent<Rigidbody>().isKinematic = false;
        thrownObject.transform.GetComponent<Rigidbody>().AddForceAtPosition(throwVelocity, transform.position, ForceMode.Impulse);

        if (hasForceDropped)
        {
            print("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            ServiceLocator.GetServiceOfType<EffectsManager>().PlayAudio("Throw", spatialBlend: 1, audioMixerGroup: "Sfx");
        }

    }

    [PunRPC()]
    private void Cast_PickupObject(int handID, int objectID, Vector3 rotation, Vector3 localPosition) 
    {
        Transform currentlyWielded = NetworkManager.GetViewByID(objectID).transform;
        Transform handToChildTo = handID == 0 ? hand.transform : h2_Anchor;

        playerAnimator.SetLayerWeight(HAND_LAYER, 1);

        currentlyWielded.transform.SetParent(handToChildTo);
        currentlyWielded.transform.localPosition = localPosition;
        currentlyWielded.transform.localEulerAngles = rotation;
        currentlyWielded.transform.GetComponent<Rigidbody>().isKinematic = true;
    }
    #endregion

    private void Awake() => playerAnimator.SetLayerWeight(HAND_LAYER, 0);

    private void Update()
    {
        CheckForInteractables();
        Interact();
    }

    private void Interact() 
    {
        const string INTERACT_BOOL_NAME = "Interacting";
        playerAnimator.SetBool(INTERACT_BOOL_NAME, false);

        #region ### When starting to interact ###
        if (currentSelected != null)
        {
            if (Input.GetKey(interactKey) && (currentSelected.GetType() == typeof(CleanableObject) || currentSelected.GetType().IsSubclassOf(typeof(CleanableObject))))
            {
                if (currentSelected.IsLocked == false && HasAccessToInteract(currentSelected))
                {
                    arrow_Selection_UX.gameObject.SetActive(false);
                    currentlyInteracting = currentSelected;
                    currentlyInteracting.Interact(this);
                    playerAnimator.SetBool(INTERACT_BOOL_NAME, true);
                    return;
                }
            }
            else if(Input.GetKeyDown(interactKey)) 
            {
                if (currentSelected.IsLocked == false && HasAccessToInteract(currentSelected))
                {
                    arrow_Selection_UX.gameObject.SetActive(false);
                    currentlyInteracting = currentSelected;
                    currentlyInteracting.Interact(this);
                }
            }
        }
        #endregion

        #region ### When Interacting ###
        if (currentlyInteracting) 
        {
            if (Input.GetKey(interactKey) == false || (currentSelected != currentlyInteracting))
            {
                currentlyInteracting.DeInteract(this);
                playerAnimator.SetBool(INTERACT_BOOL_NAME, false);
            }
        }
        #endregion

        #region ### When dropping or using your wieldable ###
        if(Input.GetKeyDown(dropWieldableKey)) 
        {
            if(currentlyWielding != null) 
            {
                currentlyWielding.DeInteract(this);

                if(currentlyInteracting) 
                {
                    currentlyInteracting.DeInteract(this);
                }
            }
        }

        if(Input.GetKeyDown(useWieldableKey)) 
        {
            HitWithWieldingObject();
        }
        #endregion
    }

    private bool isHitting = false;
    private async void HitWithWieldingObject()
    {
        if (isHitting || currentlyWielding == null)
            return;

        const string HIT_TRIGGER = "Smack";

        isHitting = true;
        playerAnimator.SetTrigger(HIT_TRIGGER);

        await Task.Delay(TimeSpan.FromSeconds(0.3f));

        if (currentlyWielding != null)
        {
            StunComponent currentStunCheck = currentlyWielding.gameObject.AddComponent<StunComponent>();
            currentStunCheck.OwningObject = currentlyWielding;
        }

        await Task.Delay(TimeSpan.FromSeconds(0.3f));

        isHitting = false;
    }

    private void CheckForInteractables() 
    {
        Ray interactableRay = new Ray(transform.position, transform.forward);
        ExtDebug.DrawBoxCastBox(transform.position, boxcast_HalfExtends, transform.rotation, transform.forward, RAY_LENGTH, Color.red);

        RaycastHit hitPoint;

        if (Physics.BoxCast(transform.position, boxcast_HalfExtends, transform.forward, out hitPoint, transform.rotation, RAY_LENGTH, interactableMask)) 
        {
            if (hitPoint.transform.GetComponent<InteractableObject>() != null) 
            {
                InteractableObject observedObject = hitPoint.transform.GetComponent<InteractableObject>();

                if (observedObject.IsLocked == false && HasAccessToInteract(observedObject))
                {
                    if (currentSelected == null) 
                    {
                        Select(observedObject);
                    } 
                    else if (currentSelected != observedObject)
                    {
                        DeSelect(currentSelected);
                        Select(observedObject);
                    }
                }
                return;
            }
        }

        if(currentSelected != null) 
        {
            DeSelect(currentSelected);
        }
    }

    private void Select(InteractableObject observedObject) 
    {
        currentSelected = observedObject;
        arrow_Selection_UX.gameObject.SetActive(true);
        arrow_Selection_UX.position = observedObject.transform.position + arrow_UX_Offset;
    }

    private void DeSelect(InteractableObject observedObject) 
    {
        currentSelected = null;
        arrow_Selection_UX.gameObject.SetActive(false);
    }

    public void PickupObject(WieldableObject wieldableObject, Vector3 localHandOffset, Vector3 localRotationOffset) 
    {
        const string HANDED_BOOL_NAME = "is2h";

        int targetViewID = wieldableObject.handedType == WieldableObject.Handed_Type.H1Handed ? 0 : 1;
        bool targetHandedState = wieldableObject.handedType != WieldableObject.Handed_Type.H1Handed;

        playerAnimator.SetBool(HANDED_BOOL_NAME, targetHandedState);

        if(currentlyWielding == null) 
        {
            currentlyWielding = wieldableObject;

            if(NetworkManager.IsConnectedAndInRoom) 
            {
                photonView.RPC(nameof(Cast_PickupObject), RpcTarget.AllBuffered,
                targetViewID,
                wieldableObject.gameObject.GetPhotonView().ViewID,
                localRotationOffset,
                localHandOffset
                );
                return;
            }

            Cast_PickupObject(
                targetViewID,
                wieldableObject.gameObject.GetPhotonView().ViewID,
                localRotationOffset,
                localHandOffset
            );
        }
    }

    public void DropObject(WieldableObject wieldableObject)
    {
        if (currentlyWielding != null) 
        {
            const int HAND_LAYER = 1;

            if (currentlyWielding.GetComponent<StunComponent>())
            {
                Destroy(currentlyWielding.GetComponent<StunComponent>());
            }

            playerAnimator.SetLayerWeight(HAND_LAYER, 0);

            currentlyWielding.UnlockObjectManually();
            currentlyWielding = null;

            if (NetworkManager.IsConnectedAndInRoom) 
            {
                photonView.RPC(nameof(Cast_ThrowObject), RpcTarget.AllBuffered, wieldableObject.gameObject.GetPhotonView().ViewID, forceDrop);

                if(currentlyInteracting) 
                {
                    currentlyInteracting.DeInteract(this);
                }

                forceDrop = false;
                return;
            }
        }
    }

    public void ForceDropObject() 
    {
        if (currentlyWielding != null) 
        {
            if (currentlyWielding.GetType() == typeof(WieldableCleanableObject))
            {
                forceDrop = true;
                DropObject(currentlyWielding);
            }
        }
    }

    public void DeinteractWithCurrentObject() 
    {
        if (currentlyInteracting != null) 
        {
            currentlyInteracting = null;
        }
    }

    #region ### Collision Checks ###
    /// <summary>
    /// The Callback is mainly used to check if you are colliding with another player, if so and if wielding a cleanable, you'll drop the object.
    /// </summary>
    /// <param name="collision"></param>
    private void OnTriggerEnter(Collider collision) 
    {
        Debug.Log(collision.gameObject.name);
        if(collision.transform.root.GetComponent<PlayerManager>()) 
        {
            PlayerManager otherPlayer = collision.transform.root.GetComponent<PlayerManager>();

            if((int)otherPlayer.team != NetworkManager.localPlayerInformation.team) 
            {
                ForceDropObject();
            }
        }   
    }
    #endregion

    #region ### Interact Checks ###
    private static bool HasAccessToInteract(InteractableObject interactableObject)
    {
        if (interactableObject.ownedByTeam == InteractableObject.OwnedByTeam.Everyone) 
        {
            return true;
        }

        if ((int)interactableObject.ownedByTeam == NetworkManager.localPlayerInformation.team)
        {
            return true;
        }

        return false;
    }
    #endregion
}
