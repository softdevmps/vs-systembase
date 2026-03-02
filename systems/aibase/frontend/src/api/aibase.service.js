import api from './axios'

const aibaseService = {
  getTemplates() {
    return api.get('/aibase/templates')
  },

  getProjects() {
    return api.get('/aibase/projects')
  },

  createProject(payload) {
    return api.post('/aibase/projects', payload)
  },

  getRuns(projectId) {
    return api.get(`/aibase/projects/${projectId}/runs`)
  },

  createRun(projectId, payload) {
    return api.post(`/aibase/projects/${projectId}/runs`, payload)
  },

  syncRun(runId) {
    return api.post(`/aibase/runs/${runId}/sync`)
  }
}

export default aibaseService
