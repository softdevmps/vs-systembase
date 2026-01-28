<template>
    <v-container fluid>
        <!-- HEADER -->
        <v-row class="mb-4">
            <v-col>
                <h2>Usuarios</h2>
                <span class="grey--text">
                    Administración de usuarios del sistema
                </span>
            </v-col>

            <v-col class="text-right">
                <v-btn color="primary" @click="abrirCrear">
                    <v-icon left>mdi-account-plus</v-icon>
                    Nuevo usuario
                </v-btn>
            </v-col>
        </v-row>

        <!-- TABLA -->
        <usuarios-table :usuarios="usuarios" :headers="headers" @editar="editar" @cambiar-estado="cambiarEstado" />

        <!-- DIALOG -->
        <usuario-dialog v-model="dialog" :usuario-id="usuarioId" :roles="roles" @guardado="cargarUsuarios" />
    </v-container>
</template>

<script>
import usuarioService from '../../api/usuario.service.js';
import rolService from '../../api/rol.service.js';

import UsuariosTable from '../../components/usuarios/UsuariosTable.vue';
import UsuarioDialog from '../../components/usuarios/UsuarioDialog.vue';

const COLUMN_TITLES = {
    username: 'Username',
    email: 'Email',
    nombreCompleto: 'Nombre',
    rol: 'Rol',
    activo: 'Activo',
};

export default {
    components: {
        UsuariosTable,
        UsuarioDialog
    },

    data() {
        return {
            usuarios: [],
            roles: [],
            headers: [],
            dialog: false,
            usuarioId: null
        };
    },

    mounted() {
        this.cargarUsuarios();
        this.cargarRoles();
    },

    methods: {
        cargarUsuarios() {
            usuarioService.obtener().then(res => {
                this.usuarios = res.data;

                if (this.headers.length === 0 && this.usuarios.length > 0) {
                    this.headers = Object.keys(this.usuarios[0])
                        .filter(key => COLUMN_TITLES[key])
                        .map(key => ({
                            key,
                            title: COLUMN_TITLES[key]
                        }));

                    this.headers.push({ key: 'actions', title: 'Acciones' });
                }
            });
        },

        cargarRoles() {
            rolService.getAll().then(res => {
                this.roles = res.data;
            });
        },

        abrirCrear() {
            this.usuarioId = null;
            this.dialog = true;
        },

        editar(item) {
            this.usuarioId = item.id;
            this.dialog = true;
        },

        cambiarEstado(item) {
            usuarioService.cambiarEstado(item.id, item.activo);
        }
    }
};
</script>
