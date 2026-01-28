<template>
    <v-card>
        <v-data-table :headers="headers" :items="usuarios">
            <template v-slot:item.activo="{ item }">
                <v-switch v-model="item.activo" inset color="green" @change="$emit('cambiar-estado', item)" />
            </template>

            <template v-slot:item.actions="{ item }">
                <v-btn icon color="primary" @click="$emit('editar', item)">
                    <v-icon>mdi-pencil</v-icon>
                </v-btn>
            </template>
        </v-data-table>
    </v-card>
</template>

<script>
export default {
    props: {
        usuarios: {
            type: Array,
            required: true
        },
        headers: {
            type: Array,
            required: true
        }
    }
};
</script>
