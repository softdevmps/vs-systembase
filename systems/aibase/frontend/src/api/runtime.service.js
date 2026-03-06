import api from './axios'

export default {
  list(route) {
    return api.get(`/${route}`)
  },

  get(route, id) {
    return api.get(`/${route}/${id}`)
  },

  create(route, payload) {
    return api.post(`/${route}`, payload)
  },

  update(route, id, payload) {
    return api.put(`/${route}/${id}`, payload)
  },

  remove(route, id) {
    return api.delete(`/${route}/${id}`)
  },

  getOverview(take = 12) {
    return api.get('/aibase/overview', { params: { take } })
  },

  listProjectRuns(projectId, take = 25) {
    return api.get(`/aibase/projects/${projectId}/runs`, { params: { take } })
  },

  getProjectWorkflow(projectId) {
    return api.get(`/aibase/projects/${projectId}/workflow`)
  },

  triggerProjectRun(projectId, runType, inputJson = null) {
    return api.post(`/aibase/projects/${projectId}/run`, { runType, inputJson })
  },

  inferProject(projectId, input, contextJson = null) {
    return api.post(`/aibase/projects/${projectId}/infer`, { input, contextJson })
  },

  assistantSuggest(prompt, stage = null, projectId = null) {
    return api.post('/aibase/assistant/suggest', { prompt, stage, projectId })
  },

  dockerStatus(stackName = null) {
    return api.get('/aibase/docker/status', { params: { stackName } })
  },

  dockerServices(stackName = null) {
    return api.get('/aibase/docker/services', { params: { stackName } })
  },

  dockerUp(payload = {}) {
    return api.post('/aibase/docker/up', payload)
  },

  dockerDown(payload = {}) {
    return api.post('/aibase/docker/down', payload)
  },

  dockerRestart(payload = {}) {
    return api.post('/aibase/docker/restart', payload)
  },

  dockerLogs({ stackName = null, service = null, tail = 200 } = {}) {
    return api.get('/aibase/docker/logs', { params: { stackName, service, tail } })
  },

  dockerServiceAction(service, action, stackName = null) {
    return api.post(`/aibase/docker/services/${encodeURIComponent(service)}/action`, { action, stackName })
  }
}
