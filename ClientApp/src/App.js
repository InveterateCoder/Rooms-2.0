import React, { Component } from 'react';
import { Context } from "./data/Context";
import { Route, Switch } from "react-router-dom";
import { Preloader } from "./Preloader";
import { Home } from "./Home";
import { Room } from "./pages/Room";
import { Get } from "./utils/requests";
import urls from "./utils/Urls";
import Countries from "./data/countries"

export default class App extends Component {
    constructor(props) {
        super(props);
        let filters = localStorage.getItem("filters");
        filters = filters ? JSON.parse(filters) : {};
        let registered = localStorage.getItem("registered");
        let lang = localStorage.getItem("lang");
        if (!lang || !registered)
            lang = this.bestLang();
        let theme = localStorage.getItem("theme") || "light";
        document.body.className = `bg-${theme}`;
        this.state = {
            jwt: localStorage.getItem("jwt"),
            registered,
            userId: 0,
            userGuid: null,
            name: "",
            room: {
                name: "",
                description: "",
                country: "gb",
                password: "",
                limit: 10
            },
            lang,
            filters,
            c_codes: registered ? localStorage.getItem("c_codes") : null,
            icon: registered ? localStorage.getItem("icon") || "user" : "user",
            perpage: registered ? localStorage.getItem("perpage") || 30 : 30,
            openin: registered ? localStorage.getItem("opin") || "nw" : "nw",
            theme
        }
    }
    bestLang = () => {
        let navlang = navigator.language.toLowerCase();
        if (navlang.startsWith("ru") ||
            navlang.startsWith("kk") || navlang.startsWith("ky") ||
            navlang.startsWith("be") || navlang.startsWith("uk") ||
            navlang.startsWith("uz") || navlang.startsWith("mo") ||
            navlang.startsWith("tk") || navlang.startsWith("tg") ||
            navlang.startsWith("ab") || navlang.startsWith("oc") ||
            navlang.startsWith("hy") || navlang.startsWith("az"))
            return "ru";
        else return "en";
    }
    signInAsGuest = (data, name, addr) => {
        localStorage.setItem("jwt", data.jwt);
        this.setState({
            jwt: data.jwt, name: name,
            userId: data.userId, userGuid: data.userGuid
        }, () => this.props.history.replace(addr || "/lobby/1"));
    }
    signInAsUser = (data, addr) => {
        localStorage.setItem("jwt", data.jwt);
        localStorage.setItem("registered", true);
        let codes = this.getCountries(this.state.filters);
        localStorage.setItem("c_codes", codes);
        this.setState({
            jwt: data.jwt,
            userId: data.userId,
            userGuid: data.userGuid,
            registered: true,
            name: data.name,
            room: !data.room ? this.state.room : {
                ...data.room,
                password: data.room.password ? data.room.password : "",
                description: data.room.description ? data.room.description : ""
            },
            lang: localStorage.getItem("lang") || this.bestLang(),
            c_codes: codes
        }, () => this.props.history.replace(addr || "/lobby/1"));
    }
    userRegistered = data => {
        localStorage.setItem("jwt", data.jwt);
        localStorage.setItem("registered", true);
        this.setState({
            jwt: data.jwt,
            registered: true,
            name: data.name
        });
    }
    changeAccaunt = (jwt, name) => {
        localStorage.setItem("jwt", jwt);
        this.setState({
            jwt: jwt,
            name: name
        });
        this.setState({ name: name });
    }
    changeIcon = icon => {
        localStorage.setItem("icon", icon);
        this.setState({
            icon: icon
        });
    }
    changeFilters = filters => {
        localStorage.setItem("filters", JSON.stringify(filters));
        let codes = this.getCountries(filters);
        localStorage.setItem("c_codes", codes);
        this.setState({ filters, c_codes: codes });
    }
    getCountries = filters => {
        let keys = Object.keys(filters);
        let c_codes = new Set();
        keys.forEach(key => {
            for (let value of Object.values(Countries))
                if (value.langs.includes(key)) c_codes.add(value.code);
        });
        let codes = "";
        if (c_codes.size > 0)
            codes = Array.from(c_codes).reduce((a, b) => a + '_' + b);
        return codes;
    }
    changeRoom = data => {
        if (data)
            this.setState({ room: data });
        else
            this.setState({
                room: {
                    name: "",
                    description: "",
                    country: "gb",
                    password: "",
                    limit: 10
                }
            });
    }
    setLanguage = lang => {
        localStorage.setItem("lang", lang);
        this.setState({ lang: lang });
    }
    setPerpage = perpage => {
        localStorage.setItem("perpage", perpage);
        this.setState({ perpage });
    }
    setOpenIn = value => {
        localStorage.setItem("opin", value);
        this.setState({ openin: value });
    }
    setTheme = theme => {
        localStorage.setItem("theme", theme);
        this.setState({ theme }, () => document.body.className = `bg-${theme}`);
    }
    signOut = () => {
        Get(urls.accountLogout, this.state.lang, this.state.jwt)
            .then(success => {
                if (success) {
                    localStorage.removeItem("jwt");
                    localStorage.removeItem("registered");
                    localStorage.removeItem("c_codes");
                    localStorage.removeItem("theme");
                    document.body.className = "bg-light";
                    this.setState({
                        jwt: null,
                        userId: 0,
                        userGuid: null,
                        registered: false,
                        lang: this.bestLang(),
                        theme: "light"
                    });
                }
            });
    }
    setM = (id, min) => {
        if (min < 2 || min > 120) return;
        this.setState({ setm: [...this.state.setm, id] },
            () => {
                localStorage.setItem('setm', JSON.stringify(this.state.setm));
                localStorage.setItem(id + '_m', Date.now() + (min * 60000));
            });
    }
    remM = (id) => {
        this.setState({ setm: this.state.setm.filter(_id => _id !== id) },
            () => {
                localStorage.setItem('setm', JSON.stringify(this.state.setm));
                localStorage.removeItem(id + '_m');
            });
    }
    setB = (id, min) => {
        if (min < 2 || min > 120) return;
        this.setState({ setb: [...this.state.setb, id] },
            () => {
                localStorage.setItem('setb', JSON.stringify(this.state.setb));
                localStorage.setItem(id + '_b', Date.now() + min * 60000);
            });
    }
    remB = (id) => {
        this.setState({ setb: this.state.setb.filter(_id => _id !== id) },
            () => {
                localStorage.setItem('setb', JSON.stringify(this.state.setb));
                localStorage.removeItem(id + '_b');
            });
    }
    render() {
        return <Context.Provider value={{
            ...this.state,
            setB: this.setB, setM: this.setM, remB: this.remB, remM: this.remM,
            signOut: this.signOut, changeFilters: this.changeFilters,
            setLanguage: this.setLanguage, changeAccaunt: this.changeAccaunt,
            changeRoom: this.changeRoom, changeIcon: this.changeIcon,
            userRegistered: this.userRegistered, signInAsUser: this.signInAsUser,
            signInAsGuest: this.signInAsGuest, setPerpage: this.setPerpage,
            setOpenIn: this.setOpenIn, setTheme: this.setTheme
        }}>
            {
                this.state.jwt && !this.state.name
                    ? <Preloader />
                    : <Switch>
                        {
                            this.state.jwt && <Route path="/room/:room" component={Room} />
                        }
                        <Route path="/" component={Home} />
                    </Switch>
            }
        </Context.Provider>
    }
    componentDidMount() {
        if (this.state.jwt) {
            Get(urls.accountInfo, this.state.lang, this.state.jwt)
                .then(data => {
                    if (data) {
                        if (this.state.registered)
                            this.setState({
                                userId: data.userId,
                                userGuid: data.userGuid,
                                name: data.name,
                                room: !data.room ? this.state.room : {
                                    ...data.room,
                                    password: data.room.password ? data.room.password : "",
                                    description: data.room.description ? data.room.description : ""
                                }
                            });
                        else this.setState({ name: data });
                    }
                    else this.signOut();
                }).catch(() => {
                    this.signOut();
                    this.props.history.replace("/fatal")
                });
        }
    }
}