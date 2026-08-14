using System.Collections;
using UnityEngine;
using Oculus.Interaction;

public class UnsnapOnGrab : MonoBehaviour
{
    [SerializeField] private SnapInteractor snapInteractor;
    [SerializeField] private PointableElement pointableElement;

    private Coroutine releaseCoroutine;

    private void OnEnable()
    {
        if (pointableElement != null)
        {
            pointableElement.WhenPointerEventRaised += HandlePointerEvent;
        }
    }

    private void OnDisable()
    {
        if (pointableElement != null)
        {
            pointableElement.WhenPointerEventRaised -= HandlePointerEvent;
        }

        snapInteractor?.ClearComputeShouldUnselectOverride();
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        // Sadece Select olaylarıyla ilgileniyoruz.
        if (evt.Type != PointerEventType.Select)
            return;

        // SnapInteractor'ın kendi oluşturduğu Select olayıysa işlem yapma.
        if (evt.Identifier == snapInteractor.Identifier)
            return;

        // Snap aktif değilse bırakılacak bir şey yok.
        if (!snapInteractor.HasSelectedInteractable)
            return;

        // Grab / Distance Grab başladığında snap'i bırak.
        snapInteractor.SetComputeShouldUnselectOverride(() => true);

        if (releaseCoroutine != null)
        {
            StopCoroutine(releaseCoroutine);
        }

        releaseCoroutine = StartCoroutine(
            WaitForSnapRelease()
        );
    }

    private IEnumerator WaitForSnapRelease()
    {
        while (snapInteractor.HasSelectedInteractable)
        {
            yield return null;
        }

        snapInteractor.ClearComputeShouldUnselectOverride();

        releaseCoroutine = null;
    }
}