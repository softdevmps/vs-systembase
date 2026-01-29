<template>
    <v-container fluid>

        <!-- HEADER -->
        <v-row class="mb-6 align-center">
            <v-col>
                <div class="d-flex align-center">
                    <v-icon class="mr-2" color="primary" size="28">
                        mdi-shield-account
                    </v-icon>

                    <div>
                        <h2 class="mb-1">Roles</h2>
                        <span class="grey--text text-body-2">
                            Administración de roles del sistema
                        </span>
                    </div>
                </div>
            </v-col>

            <v-col class="text-right">
                <v-btn color="primary" @click="nuevoRol">
                    <v-icon left>mdi-shield-plus</v-icon>
                    Nuevo rol
                </v-btn>
            </v-col>
        </v-row>

        <!-- TABLA -->
        <roles-table :roles="roles" :headers="headers" @editar="editarRol" @menus="abrirMenus"
            @cambiar-estado="cambiarEstado" />

        <!-- DIALOG CREAR / EDITAR -->
        <rol-dialog v-model="mostrarDialog" :rol="rolSeleccionado" @guardado="cargarRoles" />

        <!-- DIALOG MENUS -->
        <rol-menus-dialog v-model="mostrarMenus" :rol="rolSeleccionado" />

    </v-container>
</template>

<script>
import RolesTable from '../../components/roles/RolesTable.vue';
import RolDialog from '../../components/roles/RolDialog.vue';
import RolMenusDialog from '../../components/roles/RolMenusDialog.vue';
import rolService from '../../api/rol.service.js';

const COLUMN_TITLES = {
    nombre: 'Nombre',
    activo: 'Activo'
};

export default {
    components: {
        RolesTable,
        RolDialog,
        RolMenusDialog
    },

    data() {
        return {
            roles: [],
            headers: [],
            rolSeleccionado: null,
            mostrarDialog: false,
            mostrarMenus: false
        };
    },

    mounted() {
        this.cargarRoles();
    },

    methods: {
        async cargarRoles() {
            const res = await rolService.getAll();
            this.roles = res.data;

            if (this.headers.length === 0 && this.roles.length > 0) {
                this.headers = Object.keys(this.roles[0])
                    .filter(key => COLUMN_TITLES[key])
                    .map(key => ({
                        key,
                        title: COLUMN_TITLES[key]
                    }));

                this.headers.push({
                    key: 'actions',
                    title: 'Acciones'
                });
            }
        },

        nuevoRol() {
            this.rolSeleccionado = null;
            this.mostrarDialog = true;
        },

        editarRol(rol) {
            this.rolSeleccionado = rol;
            this.mostrarDialog = true;
        },

        abrirMenus(rol) {
            this.rolSeleccionado = rol;
            this.mostrarMenus = true;
        },

        async cambiarEstado(rol) {
            await rolService.cambiarEstado(rol.id, rol.activo);
        }
    }
};
</script>

<style scoped>
h2 {
    font-weight: 600;
}
</style>
