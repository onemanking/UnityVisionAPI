using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;
using VisionAPI;
using System.Linq;
using UnityEngine.UI;

public class Example : MonoBehaviour
{
	[SerializeField] private RawImage m_RawImage;
	[SerializeField] private Button m_SendImageButton;

	private IDisposable m_Disposable;
	private void Start()
	{
		m_SendImageButton.OnClickAsObservable()
			.Subscribe
			(
				_ =>
				{
					m_Disposable?.Dispose();

					var texture2D = m_RawImage.texture as Texture2D;

					m_Disposable = GetFacesAsObservable(texture2D)
						.Subscribe(_faceRects =>
						{
							Debug.Log("faces detected : " + _faceRects.Count);
						}).AddTo(this);
				}
			).AddTo(this);
	}

	private IObservable<List<Rect>> GetFacesAsObservable(Texture2D _texture)
	{
		return Observable.Create<List<Rect>>
		(
			_observer =>
			{
				var sendImageObservable = VisionAPIManager.Instance.SendImageAsObservable(_texture)
					.Subscribe
					(
						_response =>
						{
							var rects = (from res in _response
										 from fa in res.faceAnnotations
										 select fa.fdBoundingPoly.ConvertToRect()).ToList();

							_observer.OnNext(rects);
							_observer.OnCompleted();
						},
						_error =>
						{
							_observer.OnError(_error);
							Debug.LogError(_error.Message);
						},
						_observer.OnCompleted
					);
				return Disposable.Create(() => sendImageObservable.Dispose());
			}
		);
	}
}