import { MooAppLayout } from "@andrewmclachlan/moo-app";
import { Icon, type NavItem } from "@andrewmclachlan/moo-ds";
import { useIsAuthenticated } from "@azure/msal-react";

export const Layout = () => {
    const isAuthenticated = useIsAuthenticated();

    if (!isAuthenticated) return null;

    const sidebarMenu: NavItem[] = [
        { 
            text: "Credentials", 
            route: "/",
            image: <Icon icon="key" />,
        },
    ];

    return (
        <MooAppLayout header={{ menu: [], userMenu: [] }} sidebar={{ navItems: sidebarMenu }} />
    );
};