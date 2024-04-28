using System;
using DG.Tweening;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;

namespace FlameStream {

    public class Movement : StructuredData {

        [DataInput]
        [Label("MODE")]
        public MovementMode Mode;

        [DataInput]
        [Label("PARAMETERS")]
        public FollowMode FollowModeData;

        [DataInput]
        [Label("PARAMETERS")]
        [Hidden]
        public ElasticMode ElasticModeData;

        bool isReady;

        protected override void OnCreate() {
            base.OnCreate();
            Watch(nameof(Mode), delegate { OnModeChange(); });
        }

        protected virtual void OnReady() {
            isReady = true;
            OnModeChange();;
        }

        protected override void OnUpdate() {
            if (!isReady) {
                OnReady();
            }
            base.OnUpdate();
        }

        void OnModeChange() {
            var isFollow = Mode == MovementMode.Follow;
            var isElastic = Mode == MovementMode.Elastic;
            GetDataInputPort(nameof(FollowModeData)).Properties.hidden = !isFollow;
            GetDataInputPort(nameof(ElasticModeData)).Properties.hidden = !isElastic;
            BroadcastDataInputProperties(nameof(FollowModeData));
            BroadcastDataInputProperties(nameof(ElasticModeData));
        }

        public class FollowMode : Transition {
            public FollowMode() {
                Time = 3f;
                Ease = Ease.OutCirc;
            }
        }
        public class ElasticMode : StructuredData {
            [DataInput]
            [FloatSlider(0.01f, 10f, 0.1f)]
            [Label("SPEED_FACTOR")]
            public float SpeedFactor = 5f;

            [DataInput]
            [FloatSlider(0f, 1f, 0.01f)]
            [Label("MAX_SPEED")]
            public float MaxSpeed = 0.5f;

            [DataInput]
            [FloatSlider(0f, 0.5f, 0.001f)]
            [Label("DECELERATION")]
            public float Deceleration = 0.25f;

            [DataInput]
            [FloatSlider(0f, 0.1f, 0.001f)]
            [Label("DAMPING")]
            public float Damping = 0.03f;

            public Vector3 Velocity;
            Vector3 direction;
            float speed;

            public void UpdateVelocity(Vector3 src, Vector3 tar, float rad) {
                var d = tar - src;
                direction += d;
                direction.Normalize();
                var s = Math.Max(0, d.magnitude - rad) * SpeedFactor * Time.deltaTime;
                speed = Math.Min(MaxSpeed, Math.Max(speed, s));
            }

            protected override void OnUpdate() {
                base.OnUpdate();
                if (speed == 0) return;

                speed -= Time.deltaTime * Deceleration;
                speed *= 1 - Math.Min(1f, Math.Max(0f, Damping));
                if (speed < 0) {
                    speed = 0;
                }

                Velocity = direction * speed;
            }
        }
    }

    public enum MovementMode {
        Follow,
        Elastic,
    }
}
