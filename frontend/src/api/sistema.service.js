import api from './axios';

export default {
  getAll() {
    return api.get('/sistemas');
  },

  getById(id) {
    return api.get(`/sistemas/${id}`);
  },

  getBySlug(slug) {
    return api.get(`/sistemas/slug/${slug}`);
  },

  crear(data) {
    return api.post('/sistemas', data);
  },

  editar(id, data) {
    return api.put(`/sistemas/${id}`, data);
  },

  publicar(id) {
    return api.post(`/sistemas/${id}/publicar`);
  }
};
