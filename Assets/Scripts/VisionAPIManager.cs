using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VisionAPI
{
	public class VisionAPIManager : MonoBehaviour
	{
		private const string _API = "https://vision.googleapis.com/v1/images:annotate?key=";

		public static VisionAPIManager Instance { get { return _instance; } }
		private static VisionAPIManager _instance;

		[SerializeField] private string m_Key = "INPUT YOUR KEY";
		[SerializeField] private List<Feature> m_FeatureList;

		public enum FeatureType
		{
			LANDMARK_DETECTION,
			FACE_DETECTION,
			OBJECT_LOCALIZATION,
			LOGO_DETECTION,
			LABEL_DETECTION,
			DOCUMENT_TEXT_DETECTION,
			SAFE_SEARCH_DETECTION,
			IMAGE_PROPERTIES,
			CROP_HINTS,
			WEB_DETECTION,
		}

		private void Awake() => _instance = this.GetComponent<VisionAPIManager>();

		public IObservable<List<Response>> SendImageAsObservable(Texture2D _image)
		{
			return Observable.Create<List<Response>>
			(
				_observer =>
				{
					var bytes = _image.EncodeToPNG();
					var stringImage = System.Convert.ToBase64String(bytes);

					var postData = new PostData();

					var request = new Request();

					var image = new Image(stringImage);
					request.image = image;
					request.features = m_FeatureList;

					postData.requests = new List<Request>();
					postData.requests.Add(request);

					var json = JsonUtility.ToJson(postData);

					Debug.Log(json);

					var url = _API + m_Key;

					var postObservable = SendPostAsObservable(url, json)
						.Subscribe
						(
							_json =>
							{
								Debug.LogWarning("Vision API Res : " + _json);
								_observer.OnNext(JsonUtility.FromJson<CallbackData>(_json).responses);
								_observer.OnCompleted();
							},
							_error => Debug.LogError(_error.Message),
							_observer.OnCompleted
						);
					return Disposable.Create(() => postObservable.Dispose());
				}
			);
		}

		private IObservable<string> SendPostAsObservable(string _url, string _json)
		{
			Debug.Log($"Sending POST request to {_url}");

			Dictionary<string, string> header = new Dictionary<string, string>();
			header.Add("Content-Type", "application/json");

			byte[] data = System.Text.Encoding.UTF8.GetBytes(_json);

			return ObservableWWW.Post(_url, data, header);
		}

		public void AddFeature(Feature _feature)
		{
			Debug.Log("AddFeature : " + _feature.type + " : " + _feature.maxResults);

			if (m_FeatureList.Find(f => f.type == _feature.type).Equals(null)) m_FeatureList.Add(_feature);
		}

		public bool RemoveFeature(Feature _feature)
		{
			return m_FeatureList.Remove(_feature);
		}

		//==============================================================

		#region Post Data
		[Serializable]
		public struct PostData
		{
			public List<Request> requests;
		}

		[Serializable]
		public struct Request
		{
			public Image image;
			public List<Feature> features;
		}

		[Serializable]
		public struct Image
		{
			public string content;
			public Image(string _content) => content = _content;
		}

		[Serializable]
		public struct Feature
		{
			[JsonConverter(typeof(StringEnumConverter))]
			public FeatureType type;
			public int maxResults;

			public Feature(FeatureType _type, int _maxResults)
			{
				type = _type;
				maxResults = _maxResults;
			}
		}
		#endregion

		#region	Response data
		[Serializable]
		public struct CallbackData
		{
			public List<Response> responses;
		}

		[Serializable]
		public struct Response
		{
			public List<FaceAnnotation> faceAnnotations;
			// public List<ImagePropertiesAnnotation> imagePropertiesAnnotation;
		}

		[Serializable]
		public struct FaceAnnotation
		{
			public BoundingPoly boundingPoly;
			public BoundingPoly fdBoundingPoly;
			public List<Landmark> landmarks;
			public float rollAngle;
			public float panAngle;
			public float tiltAngle;
			public float detectionConfidence;
			public float landmarkingConfidence;
			public string joyLikelihood;
			public string sorrowLikelihood;
			public string angerLikelihood;
			public string surpriseLikelihood;
			public string underExposedLikelihood;
			public string blurredLikelihood;
			public string headwearLikelihood;
		}

		[Serializable]
		public struct BoundingPoly
		{
			public List<Vertex> vertices;

			public Rect ConvertToRect() => new Rect(x: vertices[0].x, y: vertices[0].y, width: vertices[2].x - vertices[0].x, height: vertices[2].y - vertices[0].y);
		}

		[Serializable]
		public struct Vertex
		{
			public float x;
			public float y;

			public override string ToString() => $"{x.ToString()}, {y.ToString()}";
		}

		[Serializable]
		public struct Landmark
		{
			[JsonConverter(typeof(StringEnumConverter))]
			public LandmarkType type;
			public Position position;
		}

		[Serializable]
		public enum LandmarkType
		{
			UNKNOWN_LANDMARK,
			LEFT_EYE,
			RIGHT_EYE,
			LEFT_OF_LEFT_EYEBROW,
			RIGHT_OF_LEFT_EYEBROW,
			LEFT_OF_RIGHT_EYEBROW,
			RIGHT_OF_RIGHT_EYEBROW,
			MIDPOINT_BETWEEN_EYES,
			NOSE_TIP,
			UPPER_LIP,
			LOWER_LIP,
			MOUTH_LEFT,
			MOUTH_RIGHT,
			MOUTH_CENTER,
			NOSE_BOTTOM_RIGHT,
			NOSE_BOTTOM_LEFT,
			NOSE_BOTTOM_CENTER,
			LEFT_EYE_TOP_BOUNDARY,
			LEFT_EYE_RIGHT_CORNER,
			LEFT_EYE_BOTTOM_BOUNDARY,
			LEFT_EYE_LEFT_CORNER,
			RIGHT_EYE_TOP_BOUNDARY,
			RIGHT_EYE_RIGHT_CORNER,
			RIGHT_EYE_BOTTOM_BOUNDARY,
			RIGHT_EYE_LEFT_CORNER,
			LEFT_EYEBROW_UPPER_MIDPOINT,
			RIGHT_EYEBROW_UPPER_MIDPOINT,
			LEFT_EAR_TRAGION,
			RIGHT_EAR_TRAGION,
			LEFT_EYE_PUPIL,
			RIGHT_EYE_PUPIL,
			FOREHEAD_GLABELLA,
			CHIN_GNATHION,
			CHIN_LEFT_GONION,
			CHIN_RIGHT_GONION
		};

		[Serializable]
		public struct Position
		{
			public float x;
			public float y;
			public float z;

			public override string ToString() => $"{x.ToString()}, {y.ToString()}, {z.ToString()}";

			public Vector3 ConvertToVector3() => new Vector3(x, y, z);
		}
		#endregion
	}
}
