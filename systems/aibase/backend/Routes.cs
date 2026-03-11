namespace Backend
{
    public static class Routes
    {
        public static class v1
        {
            public static class Auth
            {
                public const string Login = "api/v1/auth/login";
                public const string Registrar = "api/v1/auth/registrar";
            }

            public static class DevTools
            {
                public const string Restart = "api/v1/dev/restart";
                public const string Ping = "api/v1/dev/ping";
            }

            public static class Templates
            {
                public const string Obtener = "api/v1/templates";
                public const string ObtenerPorId = "api/v1/templates/{id}";
                public const string Crear = "api/v1/templates";
                public const string Editar = "api/v1/templates/{id}";
                public const string Eliminar = "api/v1/templates/{id}";
            }

            public static class Projects
            {
                public const string Obtener = "api/v1/projects";
                public const string ObtenerPorId = "api/v1/projects/{id}";
                public const string Crear = "api/v1/projects";
                public const string Editar = "api/v1/projects/{id}";
                public const string Eliminar = "api/v1/projects/{id}";
            }

            public static class Runs
            {
                public const string Obtener = "api/v1/runs";
                public const string ObtenerPorId = "api/v1/runs/{id}";
                public const string Crear = "api/v1/runs";
                public const string Editar = "api/v1/runs/{id}";
                public const string Eliminar = "api/v1/runs/{id}";
            }

            public static class Aibase
            {
                public const string Overview = "api/v1/aibase/overview";
                public const string RunsByProject = "api/v1/aibase/projects/{projectId}/runs";
                public const string WorkflowByProject = "api/v1/aibase/projects/{projectId}/workflow";
                public const string TriggerRun = "api/v1/aibase/projects/{projectId}/run";
                public const string DatasetUpload = "api/v1/aibase/projects/{projectId}/dataset/upload";
                public const string DatasetSources = "api/v1/aibase/projects/{projectId}/dataset/sources";
                public const string DatasetGenerate = "api/v1/aibase/projects/{projectId}/dataset/generate";
                public const string DatasetMerge = "api/v1/aibase/projects/{projectId}/dataset/merge";
                public const string RunAll = "api/v1/aibase/projects/{projectId}/run-all";
                public const string DeployAssets = "api/v1/aibase/projects/{projectId}/deploy-assets";
                public const string DeployExport = "api/v1/aibase/projects/{projectId}/deploy-export";
                public const string Bootstrap = "api/v1/aibase/bootstrap";
                public const string Infer = "api/v1/aibase/projects/{projectId}/infer";
                public const string InferMetrics = "api/v1/aibase/projects/{projectId}/infer-metrics";
                public const string AssistantSuggest = "api/v1/aibase/assistant/suggest";
                public const string DockerStatus = "api/v1/aibase/docker/status";
                public const string DockerServices = "api/v1/aibase/docker/services";
                public const string DockerUp = "api/v1/aibase/docker/up";
                public const string DockerDown = "api/v1/aibase/docker/down";
                public const string DockerRestart = "api/v1/aibase/docker/restart";
                public const string DockerLogs = "api/v1/aibase/docker/logs";
                public const string DockerServiceAction = "api/v1/aibase/docker/services/{service}/action";
            }

        }
    }
}
