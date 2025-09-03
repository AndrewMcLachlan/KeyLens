import { MooAppLayout } from "@andrewmclachlan/moo-app";
import { useIsAuthenticated } from "@azure/msal-react";

export const Layout = () => {
    const isAuthenticated = useIsAuthenticated();

    if (!isAuthenticated) return null;
    return (
        <MooAppLayout header={{ menu: [], userMenu: [] }} sidebar={{ navItems: [] }} />
    );
};