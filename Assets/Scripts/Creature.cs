﻿using DG.Tweening;
using NodeCanvas.StateMachines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


namespace Pok
{
    public class SpriteToMesh
    {
        private static readonly List<Vector3> _vertices = new List<Vector3>();
        static  Dictionary<Sprite, Mesh> cacheMesh = new Dictionary<Sprite, Mesh>();
        public static Mesh meshFromSprite(Sprite sprite)
        {
            if (cacheMesh.ContainsKey(sprite))
            {
                return cacheMesh[sprite];
            }
            var vertices = sprite.vertices; // first copy of vertex buffer occurs here, when it is marshalled from unmanaged to managed memory
            var mesh = new Mesh();

            _vertices.Clear();  // this temporary buffer allows me to avoid one of those allocation, but i still do copy of data because Mesh.SetVertices can't accept Vector2[]

            for (var i = 0; i < vertices.Length; i++)
            {
                _vertices.Add(vertices[i] * 100);
            }

            mesh.SetVertices(_vertices);  // here's the third copy of vertex buffer is created and marshalled back to unmanaged memory
            mesh.SetTriangles(sprite.triangles, 0);
            mesh.SetUVs(0, sprite.uv);
            mesh.RecalculateBounds();
            cacheMesh.Add(sprite, mesh);
            return mesh;
        }
    }
    public class Creature : BaseItem<CreatureInstanceSaved>
    {
        public UI2DSprite skin;
        protected Vector3 scale;
        protected bool effecting = false,blockMove;
        protected Coroutine timer;
        protected CreatureItem cacheItem;
        protected Tween tweenMove;
        public System.Action<CreatureInstanceSaved,CreatureItem,Creature, bool> _onPress;
      

        
        public void OnPress(bool press)
        {
            blockMove = press;
            if (tweenMove != null)
            {
                tweenMove.Kill();
            }
            _onPress?.Invoke(_info,cacheItem,this, press);
        }

    
        public override void setInfo(CreatureInstanceSaved info)
        {
            scale = GameManager.ScaleFactor;
            base.setInfo(info);
            var pInfo = GameDatabase.Instance.CreatureCollection.Find(x => x.ItemID == info.id);
            cacheItem = pInfo;
            pInfo.getSpriteForState((s) =>
            {
                skin.sprite2D = s;
                skin.MakePixelPerfect();
                skin.transform.localScale = GameManager.ScaleFactor;
                if (skin.gameObject != gameObject)
                {
                    //GetComponent<BoxCollider>().size = skin.localSize * skin.transform.localScale * transform.localScale;
                }
                
                var poly  = gameObject.AddComponent<MeshCollider>();
                if (s)
                {
                    poly.sharedMesh = SpriteToMesh.meshFromSprite(s);
                    if (_info.id.Contains("Egg") && bornAnim)
                    {
                        poly.enabled = false;
                    }
                }
            });
       
        }
        protected MapLayer map;
        private void OnEnable()
        {
            if (tweenMove != null)
            {
                tweenMove.Kill();
                tweenMove = null;
            }
            if (_info != null)
            {
                StartCoroutine(startEffect());
            }
            if(map == null)
            {
                map = GetComponentInParent<MapLayer>();
            }
        }
        public void Start()
        {
     
        }

        public IEnumerator startEffect()
        {
            float sec =UnityEngine.Random.Range(0, 5);
            yield return new WaitForSeconds(sec);
            if (cacheItem.categoryItem == CategoryItem.CREATURE)
            {
                timer = StartCoroutine(nhatien());
            }
        }

        public IEnumerator nhatien()
        {
            yield return new WaitForSeconds(5);
            timer = StartCoroutine(nhatien());
            map?.addMoney(this);
        }
        private void OnDisable()
        {
            if (tweenMove != null)
            {
                tweenMove.Kill();
                tweenMove = null;
            }
           // transform.localPosition = new Vector3(9999, 9999, 0);
            if (timer != null){
                StopCoroutine(timer);
                timer = null;
            }
            StopAllCoroutines();
        }
        protected Sequence seqEffect;
        public void effect()
        {
            effecting = true;
            NGUITools.BringForward(gameObject);
            skin.transform.localScale = scale * 0.8f;
            if(seqEffect != null)
            {
                seqEffect.Kill();
            }
            seqEffect = DOTween.Sequence();
            seqEffect.Append(skin.transform.DOScale(scale,1).SetEase(Ease.OutElastic));
            seqEffect.AppendCallback(delegate { effecting = false; seqEffect = null; });
        }
        bool bornAnim = false;
        [ContextMenu("Born")]
        public void born()
        {
            bornAnim = true;
            if (_info.id.Contains("Egg"))
            {
                skin.transform.localPosition = new Vector3(0, 1920, 0);
                if (gameObject.GetComponent<Collider>() != null)
                {
                    gameObject.GetComponent<Collider>().enabled = false;
                }
            }
            GetComponent<FSMOwner>().SendEvent("Born");
        }

        protected Vector3 oldPos;
        private void Awake()
        {
             oldPos = skin.transform.localPosition;
        }
        public void bornScale()
        {
            skin.transform.localScale = Vector3.zero;
            skin.transform.DOScale(scale, 1).SetEase(Ease.OutElastic);
        }
        public void bornDrop(System.Action onComplete = null)
        {
            effecting = true;
       
            skin.transform.localPosition = new Vector3(0, 1920, 0);
            var seq = DOTween.Sequence();
            skin.transform.localScale = new Vector3(scale.x*0.7f, scale.y , scale.z);
            seq.Append(skin.transform.DOLocalMove(oldPos, 0.8f).SetEase(Ease.InQuad));
            seq.AppendCallback(delegate { GetComponent<Collider>().enabled = true; });
            seq.Append(skin.transform.DOScale(new Vector3(scale.x, scale.y * 0.7f, scale.z), 0.25f).SetEase(Ease.OutQuad));
            seq.Append(skin.transform.DOScale(scale , 1).SetEase(Ease.OutElastic));
            seq.AppendCallback(delegate { effecting = false; onComplete?.Invoke();  });
        }

        public void move(Vector3 destiny)
        {
            if (!blockMove)
            {
                NGUITools.BringForward(gameObject);
                if(tweenMove != null)
                {
                    tweenMove.Kill();
                    tweenMove = null;
                }
                tweenMove = transform.DOLocalMove(destiny, 5).SetEase(Ease.OutExpo);
            }
        }
    }
}
