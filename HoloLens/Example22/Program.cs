using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Core;
using Urho;
using Urho.Actions;
using Urho.HoloLens;
using Urho.Shapes;
using Urho.Physics;
using Urho.Gui;

namespace Example22
{
    internal class Program
    {
        [MTAThread]
        private static void Main()
        {
            CoreApplication.Run(new AppViewSource());
        }

        class AppViewSource : IFrameworkViewSource
        {
            public IFrameworkView CreateView()
            {
                return UrhoAppView.Create<MyApplication>(null);
            }
        }
    }
    public class MyApplication : HoloApplication
    {
        public MyApplication(ApplicationOptions opts) : base(opts) { }

       
        private Node _ballNode;
        private Node _ballModelNode;

        private void CreateBall()
        {
            // Create a new node on the scene
            _ballNode = Scene.CreateChild();
            _ballNode.Position = new Vector3(0, 0, 3);
            _ballNode.Scale = new Vector3(0.1f, 0.1f, 0.1f);

            // Add the model
            _ballModelNode = _ballNode.CreateChild();
            var sphere = _ballModelNode.CreateComponent<Sphere>();
            sphere.Color = Color.Red;
        }

        private float _spawnTimer = 1;
        private float _spawnDeltaTime;

        protected override void OnUpdate(float timeStep)
        {
            base.OnUpdate(timeStep);

            // Don't do anything if the detection is activated
            if (_isSpatialMappingActive)
                return;

            _spawnDeltaTime += timeStep;
            if (_spawnDeltaTime >= _spawnTimer)
            {
                _spawnDeltaTime = 0;

                var randomPosition = new Vector3(Randoms.Next(-0.3f, 0.3f), Randoms.Next(-0.3f, 0.3f), Randoms.Next(2, 5));
                CreateBall(randomPosition);
            }
        }
        private void CreateBall(Vector3 position)
        {
            // Create a new node on the scene
            var ballNode = Scene.CreateChild();
            ballNode.Position = position;
            ballNode.Scale = new Vector3(0.1f, 0.1f, 0.1f);

            // Add the model
            var ballModelNode = ballNode.CreateChild();
            var sphere = ballModelNode.CreateComponent<Sphere>();
            sphere.Color = Color.Blue;

            // Add a rigidbody
            var rigidbody = ballNode.CreateComponent<RigidBody>();
            rigidbody.Mass = 0.1f;
            // Add a collision shape
            var collision = ballNode.CreateComponent<CollisionShape>();
            collision.SetSphere(1, Vector3.Zero, Quaternion.Identity);
        }

        private void CreateFloor()
        {
            var floorNode = Scene.CreateChild();
            floorNode.Position = new Vector3(0, -1, 0);
            floorNode.SetScale(20);

            var floorModelNode = floorNode.CreateChild();
            var plane = floorModelNode.CreateComponent<Urho.Shapes.Plane>();
            plane.Color = Color.White;

            var rigidbody = floorNode.CreateComponent<RigidBody>();
            rigidbody.Kinematic = true;

            var collision = floorNode.CreateComponent<CollisionShape>();
            collision.SetBox(new Vector3(20, 0.1f, 20), new Vector3(0, -0.05f, 0), Quaternion.Identity);
        }

        private bool _isSpatialMappingActive;

        private Node _detectedSurfaceNode;

        protected override async void Start()
        {
            base.Start();

            // Enable the AirTap gesture
            EnableGestureTapped = true;

            // Create a new node to store all the detected objects we will create
            _detectedSurfaceNode = Scene.CreateChild();
            _isSpatialMappingActive = true;

            // Start the detection
            await StartSpatialMapping(new Vector3(10, 10, 10));
        }

        public override void OnSurfaceAddedOrUpdated(SpatialMeshInfo surface, Model generatedModel)
        {
            StaticModel model;

            // If the surface already exists get its node otherwise creates a new one
            var node = _detectedSurfaceNode.GetChild(surface.SurfaceId, false);
            if (node != null)
            {
                model = node.GetComponent<StaticModel>();
            }
            else
            {
                node = _detectedSurfaceNode.CreateChild(surface.SurfaceId);
                model = node.CreateComponent<StaticModel>();
            }

            // Set the position and rotation
            node.Position = surface.BoundsCenter;
            node.Rotation = surface.BoundsRotation;

            // The model is created with the vertex data
            model.Model = CreateModelFromVertexData(surface.VertexData, surface.IndexData);

            // Add a rigidbody for the physic engine
            node.CreateComponent<RigidBody>();

            // Add a collision shape based on the model
            var shape = node.CreateComponent<CollisionShape>();
            shape.SetTriangleMesh(model.Model, 0, Vector3.One, Vector3.Zero, Quaternion.Identity);

            // Add a material for our model (a green wireframe)
            var material = Material.FromColor(Color.Green);
            material.FillMode = FillMode.Wireframe;
            model.SetMaterial(material);
        }
        public override void OnGestureTapped()
        {
            base.OnGestureTapped();

            // Stop the detection
            _isSpatialMappingActive = false;
            StopSpatialMapping();

            // Disable wireframe models but keep the rest (RigidBody, CollisionShape)
            // to interact with it
            var childCount = _detectedSurfaceNode.GetNumChildren(false);
            for (uint i = 0; i < childCount; i++)
            {
                var childNode = _detectedSurfaceNode.GetChild(i);
                var model = childNode.GetComponent<StaticModel>();
                model.Enabled = false;
            }
        }
    }
}