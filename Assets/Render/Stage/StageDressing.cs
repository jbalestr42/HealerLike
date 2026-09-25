using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace HealerLike.Render.Stage
{
    // Dresses the game scene for the render layer: adopts its camera, swaps the pipeline, lights it with the key
    // light and gives the board its ground, while the decoration the render preview replaces is hidden
    public class StageDressing : MonoBehaviour
    {
        static readonly int gridOriginId = Shader.PropertyToID("_HLGridOrigin");
        static readonly int gridCellId = Shader.PropertyToID("_HLGridCell");
        static readonly int gridExtentId = Shader.PropertyToID("_HLGridExtent");
        static readonly int gridStrengthId = Shader.PropertyToID("_HLGridStrength");
        // The board surface when the grid has no ground renderer to read it from
        static readonly float defaultSurfaceY = 0.5f;
        static readonly float farClip = 200f;

        [SerializeField] RenderPipelineAsset _pipeline;
        [SerializeField] Material _groundMaterial;
        [SerializeField] Color _backgroundColor = new Color32(191, 210, 224, 255);
        [SerializeField] Color _ambientColor = new Color(0.35f, 0.4f, 0.5f);
        [SerializeField] List<string> _hiddenObjectNames = new List<string>();
        [SerializeField] float _gridStrength = 0.12f;

        RenderPipelineAsset _previousPipeline;
        bool _isPipelineSwapped = false;
        Pose _portraitPose;
        Pose _landscapePose;

        Renderer _boardGround;
        public Renderer boardGround { get { return _boardGround; } }

        // The main camera of the scene, set up for the look; null when the scene has none
        public Camera AdoptCamera(Scene scene)
        {
            Camera gameCamera = Camera.main;
            if (gameCamera == null)
            {
                Debug.LogError($"[StageDressing] {scene.name} has no main camera to adopt.");
                return null;
            }

            // Cinemachine would overwrite the overview pose on the next frame
            CinemachineBrain brain = gameCamera.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                brain.enabled = false;
            }

            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = _backgroundColor;
            gameCamera.fieldOfView = StageCalibration.PortraitFov;
            gameCamera.nearClipPlane = StageCalibration.NearClip;
            gameCamera.farClipPlane = farClip;
            gameCamera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            return gameCamera;
        }

        public void SwapPipeline()
        {
            if (_isPipelineSwapped)
            {
                return;
            }

            _previousPipeline = QualitySettings.renderPipeline;
            QualitySettings.renderPipeline = _pipeline;
            _isPipelineSwapped = true;
        }

        public void RestorePipeline()
        {
            if (_isPipelineSwapped)
            {
                QualitySettings.renderPipeline = _previousPipeline;
                _isPipelineSwapped = false;
            }
        }

        public void SetLighting(Scene scene, Light keyLight)
        {
            foreach (Light sceneLight in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (sceneLight.type == LightType.Directional && sceneLight.gameObject.scene == scene)
                {
                    sceneLight.enabled = false;
                }
            }

            RenderSettings.sun = keyLight;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = _ambientColor;
        }

        // The board's bounds on its ground surface; the ground takes the stage material and draws the cell grid
        public Bounds SetBoard(Scene scene, GridManager grid)
        {
            Renderer groundRenderer = null;
            if (grid.ground != null)
            {
                groundRenderer = grid.ground.GetComponent<Renderer>();
            }

            _boardGround = groundRenderer;
            float surfaceY = defaultSurfaceY;
            if (groundRenderer != null)
            {
                surfaceY = groundRenderer.bounds.max.y;
                groundRenderer.sharedMaterial = _groundMaterial;
            }
            else
            {
                Debug.LogError("[StageDressing] The grid has no ground renderer, the board keeps its own look.");
            }

            Vector3 center = grid.transform.position;
            Vector3 boardSize = new Vector3(grid.width * grid.size, 0f, grid.height * grid.size);
            Bounds board = new Bounds(new Vector3(center.x, surfaceY, center.z), boardSize);
            Shader.SetGlobalVector(gridOriginId, board.min);
            Shader.SetGlobalFloat(gridCellId, grid.size);
            Shader.SetGlobalVector(gridExtentId, board.size);
            Shader.SetGlobalFloat(gridStrengthId, _gridStrength);

            foreach (string hiddenName in _hiddenObjectNames)
            {
                if (!Hide(scene, hiddenName))
                {
                    Debug.LogError($"[StageDressing] No object named {hiddenName} in {scene.name} to hide.");
                }
            }

            return board;
        }

        // The overview poses fit the board with room for the HUD
        public void FrameBoard(Bounds board)
        {
            _portraitPose = StageCalibration.PlayableFrame(board, StageCalibration.PortraitPitch,
                StageCalibration.PortraitFov, StageCalibration.PortraitAspect, StageCalibration.PortraitCentreY,
                StageCalibration.PortraitYaw);
            _landscapePose = StageCalibration.PlayableFrame(board, StageCalibration.LandscapePitch,
                StageCalibration.PortraitFov, StageCalibration.LandscapeAspect, StageCalibration.LandscapeCentreY);
        }

        public Pose OverviewPose(bool isLandscape)
        {
            if (isLandscape)
            {
                return _landscapePose;
            }

            return _portraitPose;
        }

        // Puts the camera on the overview of its orientation
        public void Frame(Camera gameCamera, bool isLandscape)
        {
            gameCamera.aspect = isLandscape ? StageCalibration.LandscapeAspect : StageCalibration.PortraitAspect;
            Pose pose = OverviewPose(isLandscape);
            gameCamera.transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        public void Clear()
        {
            Shader.SetGlobalFloat(gridStrengthId, 0f);
            _boardGround = null;
        }

        bool Hide(Scene scene, string objectName)
        {
            bool isFound = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    // The scene's far ground shares the board ground's name, the board itself stays
                    bool isNamed = child.name == objectName;
                    if (isNamed && child.TryGetComponent(out Renderer hidden) && hidden != _boardGround)
                    {
                        hidden.enabled = false;
                        isFound = true;
                    }
                }
            }

            return isFound;
        }
    }
}
