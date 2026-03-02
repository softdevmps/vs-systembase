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
  }
}

export default aibaseService
