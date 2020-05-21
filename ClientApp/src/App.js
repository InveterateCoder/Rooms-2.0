import React, { Component } from 'react';
import { Context } from "./data/Context";
import { Route, Switch } from "react-router-dom";
import { Preloader } from "./Preloader";
import { Home } from "./Home";
import { Room } from "./pages/Room";
import { Get } from "./utils/requests";
import urls from "./utils/Urls";
import Countries from "./data/countries"

function isMobileTablet() {
    var check = false;
    (function (a) {
        //eslint-disable-next-line
        if (/(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino|android|ipad|playbook|silk/i.test(a) || /1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-/i.test(a.substr(0, 4)))
            check = true;
    })(navigator.userAgent || navigator.vendor || window.opera);
    return check;
}

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
        let isMobile = isMobileTablet();
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
            openin: isMobile ? "st" : "nw",
            theme,
            isMobile
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
    render() {
        return <Context.Provider value={{
            ...this.state,
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
                    }
                    else this.signOut();
                }).catch(() => {
                    this.signOut();
                    this.props.history.replace("/fatal")
                });
        }
    }
}